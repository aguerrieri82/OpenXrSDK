#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using Silk.NET.OpenXR;
using OpenXr.Framework;
using XrEngine.OpenGL;
using XrEngine.Filament;
using System.Numerics;
using XrMath;
using XrEngine.UI;
using static XrEngine.Filament.FilamentLib;
using System.Diagnostics;
using OpenXr.Framework.Oculus;
using Common.Interop;

namespace XrEngine.OpenXr
{
    public unsafe static class XrExtensions
    {
        public delegate IGlRenderTarget GlRenderTargetFactory(GL gl, uint colorTex, uint depthTex);

        public static void CreateOverlay(this CanvasView3D canvas, XrApp app)
        {
            canvas.AddComponent(new XrQuodAttached(app));

        }

        public static GetQuadDelegate BindToQuad(this TriangleMesh mesh)
        {
            return () =>
            {
                var result = new Quad3
                {
                    Size = new Vector2(mesh.Transform.Scale.X, mesh.Transform.Scale.Y),
                    Pose = new Pose3
                    {
                        Orientation = mesh.WorldOrientation,
                        Position = mesh.WorldPosition
                    }
                };

                return result;
            };
        }

        public static IRenderEngine BindEngineApp(this XrApp xrApp, EngineApp app, XrProjDepthMode depthMode)
        {
            if (app.Renderer is OpenGLRender openGl)
            {
                if (openGl.Options.UseResolve)
                    return xrApp.BindEngineAppGLResolve(app);
                else
                    return xrApp.BindEngineAppGL(app, depthMode);
            }

            if (app.Renderer is FilamentRender)
                return xrApp.BindEngineAppFl(app);

            throw new NotSupportedException();
        }

        public static FilamentRender BindEngineAppFl(this XrApp xrApp, EngineApp app)
        {
            var renderer = (FilamentRender)app.Renderer!;

            var headViews = new View[2];
            for (var i = 0; i < 2; i++)
                headViews[i].Type = StructureType.View;

            void RenderView(ref RenderViewInfo info)
            {
                nint colorImagePtr;
                nint depthImagePtr;
                FlTextureInternalFormat format;

                var depthImages = info.DepthImages;
                var colorImages = info.ColorImages;

                void GetImage(int imgIndex)
                {
                    if (renderer.Driver == FlBackend.OpenGL)
                    {
                        colorImagePtr = (nint)((SwapchainImageOpenGLKHR*)colorImages[imgIndex])->Image;
                        depthImagePtr = depthImages == null ? 0 : (nint)((SwapchainImageOpenGLKHR*)depthImages[imgIndex])->Image;
                        format = ((GLEnum)(int)xrApp.RenderOptions.ColorFormat) switch
                        {
                            GLEnum.Srgb8Alpha8 => FlTextureInternalFormat.SRGB8_A8,
                            GLEnum.Rgba8 => FlTextureInternalFormat.RGBA8,
                            _ => throw new NotSupportedException()
                        };
                    }
                    else
                    {
                        colorImagePtr = (nint)((SwapchainImageVulkanKHR*)colorImages[imgIndex])->Image;
                        depthImagePtr = depthImages == null ? 0 : (nint)((SwapchainImageVulkanKHR*)depthImages[imgIndex])->Image;
                        format = ((Silk.NET.Vulkan.Format)(int)xrApp.RenderOptions.ColorFormat) switch
                        {
                            Silk.NET.Vulkan.Format.R8G8B8A8Srgb => FlTextureInternalFormat.SRGB8_A8,
                            Silk.NET.Vulkan.Format.R8G8B8A8Unorm => FlTextureInternalFormat.RGBA8,
                            _ => throw new NotSupportedException()
                        };
                    }
                }

                var camera = (PerspectiveCamera)app.ActiveScene!.ActiveCamera!;

                if (info.Mode == XrRenderMode.SingleEye)
                {
                    for (var i = 0; i < info.ColorImages.Length; i++)
                    {
                        GetImage(i);

                        var rect = info.ProjViews[i].SubImage.ImageRect.Convert().To<Rect2I>();

                        renderer.SetRenderTarget(
                            rect.Width,
                            rect.Height,
                            colorImagePtr,
                            depthImagePtr,
                            format);

                        var transform = XrCameraTransform.FromView(info.ProjViews[i], camera.Near, camera.Far);

                        camera.Projection = transform.Projection;
                        camera.WorldMatrix = transform.Transform;
                        camera.ViewSize = rect.Size;

                        var depth = (CompositionLayerDepthInfoKHR*)info.ProjViews[0].Next;
                        if (depth != null)
                        {
                            depth->NearZ = camera.Near;
                            depth->FarZ = camera.Far;
                        }

                        if (i == 0)
                            app.RenderFrame();
                        else
                            renderer.Render(app.RenderContext, rect, true);
                    }
                }
                else
                {
                    GetImage(0);

                    var rect = info.ProjViews[0].SubImage.ImageRect.Convert().To<Rect2I>();

                    if (info.Mode == XrRenderMode.Stereo)
                        rect.Width *= 2;

                    renderer.SetRenderTarget(
                        rect.Width,
                        rect.Height,
                        colorImagePtr,
                        depthImagePtr,
                        format);

                    camera.Eyes ??= new CameraEye[2];

                    //TODO improve head-rel views VS space-rel views xrApp.Stage is HARDCODED
                    var headLoc = xrApp.SpacesTracker.GetLastLocation(xrApp.Head);

                    Debug.Assert(headLoc != null);

                    xrApp!.LocateViews(xrApp.Head, info.DisplayTime, headViews);

                    camera.WorldMatrix = (Matrix4x4.CreateFromQuaternion(headLoc.Pose.Orientation) *
                                          Matrix4x4.CreateTranslation(headLoc.Pose.Position));

                    for (var i = 0; i < info.ProjViews.Length; i++)
                    {
                        var transform = XrCameraTransform.FromView(headViews[i], camera.Near, camera.Far);

                        camera.Eyes[i].World = transform.Transform;
                        camera.Eyes[i].Projection = transform.Projection;

                        var depth = (CompositionLayerDepthInfoKHR*)info.ProjViews[0].Next;
                        if (depth != null)
                        {
                            depth->NearZ = camera.Near;
                            depth->FarZ = camera.Far;
                        }
                    }

                    camera.ViewSize = rect.Size;
                    app.RenderFrame(camera);
                }
            }

            xrApp.Layers.AddProjection(RenderView, xrApp.RenderOptions.UseProjectionDepth);

            return renderer;
        }

        public static OpenGLRender BindEngineAppGL(this XrApp xrApp, EngineApp app, XrProjDepthMode depthMode)
        {
            var pool = new GlRenderTargetPool(OpenGLRender.Current!.GL,
                           xrApp.RenderOptions.RenderMode == XrRenderMode.MultiView);

            xrApp.SessionChanged += (s, e) =>
            {
                if (xrApp.State == XrAppState.Stopped)
                    pool.Clear();
            };

            GlDepthExportPass? depthExportPass = null;

            GlDepthCopyPass? depthCopyPass = null;

            return xrApp.BindEngineAppGL(app, (gl, colorTex, depthTex) =>
            {
                var sampleCount = xrApp.RenderOptions.SampleCount;
                var isMultiView = xrApp.RenderOptions.RenderMode == XrRenderMode.MultiView;

                var renderer = OpenGLRender.Current;

                var isHandleDepth = depthTex != 0 && sampleCount > 1;

                if (isHandleDepth)
                {
                    if (depthMode == XrProjDepthMode.DepthPass)
                    {
                        depthExportPass ??= renderer.EnsurePass(() => new GlDepthExportPass(renderer, isMultiView));

                        depthExportPass.Configure(depthTex);
                    }

                    else if (depthMode == XrProjDepthMode.DepthCopy)
                    {
                        depthCopyPass ??= renderer.EnsurePass(() => new GlDepthCopyPass(renderer, isMultiView, false));

                        depthCopyPass.Configure(depthTex);

                        renderer.UpdateContext.UseCopyDepth = true;
                    }
                    else if (depthMode == XrProjDepthMode.DepthCopyImage)
                    {
                        depthCopyPass ??= renderer.EnsurePass(() => new GlDepthCopyPass(renderer, isMultiView, true));

                        renderer.UpdateContext.CopyDepthImage = (Texture2D?)depthCopyPass
                            .Configure(depthTex)?
                            .ToEngineTexture();
                    }

                    depthTex = 0;
                }

                var renderTarget = pool.GetRenderTarget(colorTex, depthTex, xrApp.RenderOptions.SampleCount);

                if (depthMode == XrProjDepthMode.DepthCopy && isHandleDepth)
                {
                    renderTarget.FrameBuffer.GetOrCreateEffect(FramebufferAttachment.ColorAttachment1, TextureFormat.GrayInt16);

                    renderTarget.FrameBuffer.BindDraw(DrawBufferMode.ColorAttachment0, DrawBufferMode.ColorAttachment1);

                    renderer.State.SetWriteColor(true);
                    renderer.GL.Disable(EnableCap.Blend, 1);
                    renderer.GL.ClearBuffer(BufferKind.Color, 1, [1f]);
                }

                return renderTarget;
            });

        }

        public static OpenGLRender BindEngineAppGLResolve(this XrApp xrApp, EngineApp app)
        {
            var swap = new GlResolveRenderTarget(OpenGLRender.Current!.GL,
                xrApp.RenderOptions.RenderMode == XrRenderMode.MultiView,
                XrEngineApp.Current!.Options.SampleCount
            );

            xrApp.SessionChanged += (s, e) =>
            {
                if (xrApp.State == XrAppState.Stopped)
                    swap.Clear();
            };

            return xrApp.BindEngineAppGL(app, (gl, colorTex, depthTex) =>
            {
                swap.Select(colorTex, depthTex);
                return swap;
            });
        }

        public static OpenGLRender BindEngineAppGL(this XrApp xrApp, EngineApp app, GlRenderTargetFactory targetFactory)
        {
            OpenGLRender renderer;

            if (!app.HasRenderer)
            {
                var driver = xrApp.Plugin<IXrGraphicDriver>();

                if (driver is not IApiProvider apiProvider)
                    throw new NotSupportedException();

                var gl = apiProvider.GetApi<GL>() ??
                    throw new NotSupportedException();

                renderer = new OpenGLRender(gl);
                app.Renderer = renderer;
            }
            else
                renderer = (OpenGLRender)app.Renderer;

            void RenderView(ref RenderViewInfo info)
            {
                var camera = (PerspectiveCamera)app.ActiveScene!.ActiveCamera!;

                camera.Eyes ??= new CameraEye[2];
                camera.IsStereo = true;
                camera.IsMultiView = info.Mode == XrRenderMode.MultiView;
                camera.Transform.Version++;

                var eyes = camera.Eyes;

                for (var i = 0; i < info.ProjViews.Length; i++)
                {
                    var transform = XrCameraTransform.FromView(info.ProjViews[i], camera.Near, camera.Far);

                    eyes[i].World = transform.Transform * XrApp.Current!.ReferenceFrame.ToMatrix();
                    eyes[i].Projection = transform.Projection;
                    eyes[i].View = eyes[i].World.Invert();
                    eyes[i].ViewProj = eyes[i].View * eyes[i].Projection;
                    eyes[i].ViewProjInv = eyes[i].ViewProj.Invert();
                }

                if (info.Mode == XrRenderMode.SingleEye)
                {
                    app.BeginFrame();

                    for (var i = 0; i < info.ColorImages.Length; i++)
                    {
                        var rect = info.ProjViews[i].SubImage.ImageRect.Convert().To<Rect2I>();

                        var glColorImage = ((SwapchainImageOpenGLKHR*)info.ColorImages[i])->Image;
                        var glDepthImage = info.DepthImages == null ? 0 : ((SwapchainImageOpenGLKHR*)info.DepthImages[i])->Image;

                        var renderTarget = targetFactory(renderer.GL, glColorImage, glDepthImage);

                        camera.SetProp(OpenGLRender.Props.RenderTarget[i], renderTarget);

                        renderer.SetRenderTarget(renderTarget);

                        camera.Projection = eyes[i].Projection;
                        camera.WorldMatrix = eyes[i].World;
                        camera.ActiveEye = i;
                        camera.ViewSize = rect.Size;

                        var depth = (CompositionLayerDepthInfoKHR*)StructChain.FindNextStruct(ref info.ProjViews[i], StructureType.CompositionLayerDepthInfoKhr);

                        if (depth != null)
                        {
                            depth->NearZ = camera.Near;
                            depth->FarZ = camera.Far;
                        }

                        app.RenderScene(null, false);
                    }

                    app.EndFrame();
                }
                else if (info.Mode == XrRenderMode.MultiView)
                {
                    var rect = info.ProjViews[0].SubImage.ImageRect.Convert().To<Rect2I>();

                    var glColorImage = ((SwapchainImageOpenGLKHR*)info.ColorImages[0])->Image;
                    var glDepthImage = info.DepthImages == null ? 0 : ((SwapchainImageOpenGLKHR*)info.DepthImages[0])->Image;

                    var renderTarget = targetFactory(renderer.GL, glColorImage, glDepthImage);

                    camera.SetProp(OpenGLRender.Props.RenderTarget[0], renderTarget);

                    renderer.SetRenderTarget(renderTarget);

                    camera.Projection = eyes[0].Projection;
                    camera.WorldMatrix = eyes[0].World.InterpolateWorldMatrix(eyes[1].World, 0.5f);

                    camera.ViewSize = rect.Size;
                    camera.ActiveEye = -1;

                    var depth = (CompositionLayerDepthInfoKHR*)StructChain.FindNextStruct(ref info.ProjViews[0], StructureType.CompositionLayerDepthInfoKhr);

                    if (depth != null)
                    {
                        depth->NearZ = camera.Near;
                        depth->FarZ = camera.Far;
                    }

                    app.RenderFrame(null, false);
                }
            }

            var useDepth = xrApp.RenderOptions.UseProjectionDepth;

            GlMotionVectorPass? motionVectorPass = null;

            if (renderer.Options.MotionVectorMode == MotionVectorMode.Pass)
            {
                motionVectorPass ??= renderer.EnsurePass(() => new GlMotionVectorPass(
                            renderer, xrApp,
                            xrApp.RenderOptions.RenderMode == XrRenderMode.MultiView));

                var provider = new GlMotionVectorProvider(app, renderer, motionVectorPass);

                xrApp.Layers.Add(new XrSpaceWarpProjectionLayerV2(RenderView, provider));
            }
            else if (renderer.Options.MotionVectorMode == MotionVectorMode.Shared)
            {
                var provider = new GlMotionVectorProviderV2(app, renderer);

                xrApp.Layers.Add(new XrSpaceWarpProjectionLayerV2(RenderView, provider));
            }
            else
                xrApp.Layers.AddProjection(RenderView, useDepth);

            return renderer;
        }

        public static IEnumerable<Quad3> GetWallsPlanes(this OculusSceneView self)
        {
            foreach (var wall in self.Children.Where(a => a.Name == "Wall"))
            {
                if (!wall.IsVisible)
                    continue;

                var mesh = (TriangleMesh)wall;
                var cube = (Cube3D)mesh.Geometry!;

                yield return new Quad3
                {
                    Size = new Vector2(cube.Size.X, cube.Size.Y),
                    Pose = new Pose3
                    {
                        Orientation = mesh.WorldOrientation,
                        Position = mesh.WorldPosition - mesh.Forward * cube.Size.Z / 2
                    }
                };
            }
        }
    }
}
