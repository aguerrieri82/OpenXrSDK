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


namespace XrEngine.OpenXr
{
    public unsafe static class XrExtensions
    {
        public delegate IGlRenderTarget GlRenderTargetFactory(GL gl, uint colorTex, uint depthTex);

        public static void CreateOverlay(this CanvasView3D canvas, XrApp app)
        {
            canvas.Mode = CanvasViewMode.RenderTarget;

            var layer = new XrTextureQuadLayer(canvas.BindToQuad(), (image, size, predTime) =>
            {
                if (image == null)
                    return canvas.NeedDraw;

                //TODO handle vulkan
                var glImage = (SwapchainImageOpenGLKHR*)image;

                canvas.SetRenderTarget((nint)glImage->Image, size.Width, size.Height);
                canvas.Draw();

                return true;

            }, canvas.PixelSize);

            layer.Priority = 5;

            app.Layers.Add(layer);
        }

        public static GetQuadDelegate BindToQuad(this TriangleMesh mesh)
        {
            return () =>
            {
                var result = new Quad3
                {
                    //IsVisible = mesh.IsVisible && mesh.Parent != null,
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

        public static IRenderEngine BindEngineApp(this XrApp xrApp, EngineApp app)
        {
            if (app.Renderer is OpenGLRender || app.Renderer == null)
                return xrApp.BindEngineAppGL(app);

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

                        var depth = (CompositionLayerDepthInfoKHR*)info.ProjViews[0].Next;
                        if (depth != null)
                        {
                            depth->NearZ = camera.Near;
                            depth->FarZ = camera.Far;
                        }

                        if (i == 0)
                            app.RenderFrame(rect, false);
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

                    app.RenderFrame(rect);
                }
            }

            xrApp.Layers.AddProjection(RenderView);

            return renderer;
        }

        public static OpenGLRender BindEngineAppGL(this XrApp xrApp, EngineApp app)
        {
            var pool = new GlFrameBufferPool(OpenGLRender.Current!.GL,
                           xrApp.RenderOptions.RenderMode == XrRenderMode.MultiView);

            xrApp.SessionChanged += (s, e) =>
            {
                if (xrApp.State == XrAppState.Stopped)
                    pool.Clear();
            };

            return xrApp.BindEngineAppGL(app, (gl, colorTex, depthTex) =>
                pool.GetRenderTarget(colorTex, xrApp.RenderOptions.SampleCount));
        }

        public static OpenGLRender BindEngineAppGL(this XrApp xrApp, EngineApp app, GlRenderTargetFactory targetFactory)
        {
            OpenGLRender renderer;

            if (app.Renderer == null)
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
                camera.Transform.Version++;

                for (var i = 0; i < info.ProjViews.Length; i++)
                {
                    var transform = XrCameraTransform.FromView(info.ProjViews[i], camera.Near, camera.Far);

                    camera.Eyes[i].World = transform.Transform * XrApp.Current!.ReferenceFrame.ToMatrix();
                    camera.Eyes[i].Projection = transform.Projection;
                    Matrix4x4.Invert(transform.Transform, out camera.Eyes[i].View);
                    camera.Eyes[i].ViewProj = camera.Eyes[i].View * camera.Eyes[i].Projection;
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

                        renderer.SetRenderTarget(renderTarget);

                        camera.Projection = camera.Eyes[i].Projection;
                        camera.WorldMatrix = camera.Eyes[i].World;
                        camera.ActiveEye = i;

                        var depth = (CompositionLayerDepthInfoKHR*)StructChain.FindNextStruct(ref info.ProjViews[i], StructureType.CompositionLayerDepthInfoKhr);

                        if (depth != null)
                        {
                            depth->NearZ = camera.Near;
                            depth->FarZ = camera.Far;
                        }

                        app.RenderScene(rect, false);
                    }

                    app.EndFrame();
                }
                else if (info.Mode == XrRenderMode.MultiView)
                {
                    var rect = info.ProjViews[0].SubImage.ImageRect.Convert().To<Rect2I>();

                    var glColorImage = ((SwapchainImageOpenGLKHR*)info.ColorImages[0])->Image;
                    var glDepthImage = info.DepthImages == null ? 0 : ((SwapchainImageOpenGLKHR*)info.DepthImages[0])->Image;

                    var renderTarget = targetFactory(renderer.GL, glColorImage, glDepthImage);

                    renderer.SetRenderTarget(renderTarget);

                    camera.Projection = camera.Eyes[0].Projection;
                    camera.WorldMatrix = camera.Eyes[0].World;

                    var depth = (CompositionLayerDepthInfoKHR*)StructChain.FindNextStruct(ref info.ProjViews[0], StructureType.CompositionLayerDepthInfoKhr);

                    if (depth != null)
                    {
                        depth->NearZ = camera.Near;
                        depth->FarZ = camera.Far;
                    }

                    app.RenderFrame(rect);
                }
            }

            if (renderer.HasPass<GlMotionVectorPass>())
            {
                var provider = new GlMotionVectorProvider(app, renderer);
                provider.IsActive = true;
                Context.Implement<IMotionVectorProvider>(provider);
                xrApp.Layers.Add(new XrSpaceWarpProjectionLayer(RenderView, provider));
            }
            else
                xrApp.Layers.AddProjection(RenderView);

            return renderer;
        }


        public static IEnumerable<Quad3> GetWallsPlanes(this OculusSceneView self)
        {
            foreach (var wall in self.Children.Where(a => a.Name == "Wall"))
            {
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
