#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using OpenXr.Engine;
using OpenXr.Engine.OpenGL;
using OpenXr.Engine.OpenGL.Oculus;
using Silk.NET.OpenXR;


namespace OpenXr.Framework
{
    public unsafe static class XrAppExtensions
    {
        public delegate IGlRenderTarget GlRenderTargetFactory(GL gl, uint texId);

        public static void BindEngineApp(this XrApp xrApp, EngineApp app, uint sampleCount = 1, bool multiView = false)
        {
            GlRenderTargetFactory factory;

            if (multiView)
                factory = (gl, texId) => GlMultiViewRenderTarget.Attach(gl, texId, sampleCount);
            else
            {
                if (sampleCount == 1)
                    factory = GlTextureRenderTarget.Attach;
                else
                    factory = (gl, texId) => GlMultiSampleRenderTarget.Attach(gl, texId, sampleCount);
            }

            xrApp.BindEngineApp(app, factory, multiView);
        }

        public static void BindEngineApp(this XrApp xrApp, EngineApp app, GlRenderTargetFactory targetFactory, bool multiView)
        {
            var driver = xrApp.Plugin<IXrGraphicDriver>();

            if (driver is not IApiProvider apiProvider)
                throw new NotSupportedException();

            var gl = apiProvider.GetApi<GL>();

            if (gl == null)
                throw new NotSupportedException();

            var renderer = new OpenGLRender(gl);
            renderer.EnableDebug();
            app.Renderer = renderer;

            app.Start();

            void RenderView(ref CompositionLayerProjectionView view, SwapchainImageBaseHeader* image, int viewIndex, long predTime)
            {
                var glImage = (SwapchainImageOpenGLKHR*)image;

                var rect = view.SubImage.ImageRect.Convert().To<RectI>();

                var renderTarget = targetFactory(gl, glImage->Image);

                renderer.SetRenderTarget(renderTarget);

                var camera = (PerspectiveCamera)app.ActiveScene!.ActiveCamera!;

                var transform = XrCameraTransform.FromView(view, camera.Near, camera.Far);

                camera.Projection = transform.Projection;
                camera.Transform.SetMatrix(transform.View);

                if (viewIndex == 0)
                    app.RenderFrame(rect);
                else
                {
                    camera.UpdateWorldMatrix(false, false);
                    app.Renderer.Render(app.ActiveScene, camera, rect);
                }
            }

            void RenderMultiView(ref Span<CompositionLayerProjectionView> views, SwapchainImageBaseHeader* image, long predTime)
            {
                var glImage = (SwapchainImageOpenGLKHR*)image;

                var rect = views[0].SubImage.ImageRect.Convert().To<RectI>();

                var renderTarget = targetFactory(gl, glImage->Image);

                if (renderTarget is not IMultiViewTarget multiTarget)
                    throw new NotSupportedException("Render target don't support multi-view");

                renderer.SetRenderTarget(renderTarget);

                var camera = (PerspectiveCamera)app.ActiveScene!.ActiveCamera!;

                var transforms = new XrCameraTransform[views.Length];
                for (var i = 0; i < transforms.Length; i++)
                    transforms[i] = XrCameraTransform.FromView(views[i], camera.Near, camera.Far);

                camera.Projection = transforms[0].Projection;
                camera.Transform.SetMatrix(transforms[0].View);

                multiTarget.SetCameraTransforms(transforms);

                app.RenderFrame(rect);
            }

            if (multiView)
                xrApp.Layers.AddProjection(RenderMultiView);
            else
                xrApp.Layers.AddProjection(RenderView);

            return;

        }
    }
}
