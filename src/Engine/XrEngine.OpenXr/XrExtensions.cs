#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using Silk.NET.OpenXR;
using OpenXr.Framework;
using XrEngine.OpenGL;
using XrEngine.OpenGL.Oculus;
using XrEngine.Filament;
using System.Numerics;
using XrMath;
using XrEngine.UI;
using static XrEngine.Filament.FilamentLib;


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

            void RenderView(ref Span<CompositionLayerProjectionView> views, SwapchainImageBaseHeader*[] colorImages, SwapchainImageBaseHeader*[]? depthImages, XrRenderMode mode, long predTime)
            {
                nint colorImagePtr;
                nint depthImagePtr;
                FlTextureInternalFormat format;

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

                if (mode == XrRenderMode.SingleEye)
                {
                    for (var i = 0; i < colorImages.Length; i++)
                    {
                        GetImage(i);

                        var rect = views[i].SubImage.ImageRect.Convert().To<Rect2I>();

                        renderer.SetRenderTarget(
                            rect.Width,
                            rect.Height,
                            colorImagePtr,
                            depthImagePtr,
                            format);

                        var transform = XrCameraTransform.FromView(views[i], camera.Near, camera.Far);

                        camera.Projection = transform.Projection;
                        camera.WorldMatrix = transform.Transform;

                        var depth = (CompositionLayerDepthInfoKHR*)views[0].Next;
                        if (depth != null)
                        {
                            depth->NearZ = camera.Near;
                            depth->FarZ = camera.Far;
                        }

                        if (i == 0)
                            app.RenderFrame(rect, false);
                        else
                            renderer.Render(app.ActiveScene, camera, rect, true);
                    }
                }
                else
                {
                    GetImage(0);

                    var rect = views[0].SubImage.ImageRect.Convert().To<Rect2I>();

                    if (mode == XrRenderMode.Stereo)
                        rect.Width *= 2;

                    renderer.SetRenderTarget(
                        rect.Width,
                        rect.Height,
                        colorImagePtr,
                        depthImagePtr,
                        format);

                    camera.Eyes ??= new CameraEye[2];

                    //TODO improve head-rel views VS space-rel views xrApp.Stage is HARDCODED
                    var headLoc = xrApp.LocateSpace(xrApp.Head, xrApp.Stage, predTime);

                    xrApp!.LocateViews(xrApp.Head, predTime, headViews);

                    camera.WorldMatrix = (Matrix4x4.CreateFromQuaternion(headLoc.Pose.Orientation) *
                                          Matrix4x4.CreateTranslation(headLoc.Pose.Position));


                    for (var i = 0; i < views.Length; i++)
                    {
                        var transform = XrCameraTransform.FromView(headViews[i], camera.Near, camera.Far);

                        camera.Eyes[i].Transform = transform.Transform;
                        camera.Eyes[i].Projection = transform.Projection;

                        var depth = (CompositionLayerDepthInfoKHR*)views[0].Next;
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
            GlRenderTargetFactory factory;

            GlTextureRenderTarget? texTarget = null;
            GlMultiViewRenderTarget? mvTexTarget = null;

            if (xrApp.RenderOptions.RenderMode == XrRenderMode.MultiView)
                factory = (gl, colorTex, depthTex) =>
                {
                    mvTexTarget ??= new GlMultiViewRenderTarget(gl);
                    mvTexTarget.FrameBuffer.Configure(colorTex, depthTex, xrApp.RenderOptions.SampleCount);
                    return mvTexTarget;
                };
            else
            {
                factory = (gl, colorTex, depthTex) =>
                {
                    texTarget ??= new GlTextureRenderTarget(gl);
                    texTarget.FrameBuffer.Configure(colorTex, depthTex, xrApp.RenderOptions.SampleCount);
                    return texTarget;
                };
            }

            return xrApp.BindEngineAppGL(app, factory);
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


            void RenderView(ref Span<CompositionLayerProjectionView> views, SwapchainImageBaseHeader*[] colorImages, SwapchainImageBaseHeader*[]? depthImages, XrRenderMode mode, long predTime)
            {
                var camera = (PerspectiveCamera)app.ActiveScene!.ActiveCamera!;

                if (mode == XrRenderMode.SingleEye)
                {
                    for (var i = 0; i < colorImages.Length; i++)
                    {
                        var rect = views[i].SubImage.ImageRect.Convert().To<Rect2I>();

                        var glColorImage = ((SwapchainImageOpenGLKHR*)colorImages[i])->Image;
                        var glDepthImage = depthImages == null ? 0 : ((SwapchainImageOpenGLKHR*)depthImages[i])->Image;

                        var renderTarget = targetFactory(renderer.GL, glColorImage, glDepthImage);

                        renderer.SetRenderTarget(renderTarget);

                        var transform = XrCameraTransform.FromView(views[i], camera.Near, camera.Far);

                        camera.Projection = transform.Projection;
                        camera.WorldMatrix = transform.Transform;
                        camera.ActiveEye = i;

                        var depth = (CompositionLayerDepthInfoKHR*)views[0].Next;
                        if (depth != null)
                        {
                            depth->NearZ = camera.Near;
                            depth->FarZ = camera.Far;
                        }

                        if (i == 0)
                            app.RenderFrame(rect, false);
                        else
                            app.Renderer.Render(app.ActiveScene, camera, rect, true);

                    }
                }
                else if (mode == XrRenderMode.MultiView)
                {
                    var rect = views[0].SubImage.ImageRect.Convert().To<Rect2I>();

                    var glColorImage = ((SwapchainImageOpenGLKHR*)colorImages[0])->Image;
                    var glDepthImage = depthImages == null ? 0 : ((SwapchainImageOpenGLKHR*)depthImages[0])->Image;

                    var renderTarget = targetFactory(renderer.GL, glColorImage, glDepthImage);

                    if (renderTarget is not IMultiViewTarget multiTarget)
                        throw new NotSupportedException("Render target don't support multi-view");

                    var transforms = new XrCameraTransform[views.Length];

                    for (var i = 0; i < transforms.Length; i++)
                        transforms[i] = XrCameraTransform.FromView(views[i], camera.Near, camera.Far);

                    multiTarget.SetCameraTransforms(transforms, camera.Far);

                    renderer.SetRenderTarget(renderTarget);

                    camera.Projection = transforms[0].Projection;
                    camera.WorldMatrix = transforms[0].Transform;

                    var depth = (CompositionLayerDepthInfoKHR*)views[0].Next;
                    if (depth != null)
                    {
                        depth->NearZ = camera.Near;
                        depth->FarZ = camera.Far;
                    }

                    app.RenderFrame(rect);
                }
            }

            xrApp.Layers.AddProjection(RenderView);

            return renderer;
        }
    }
}
