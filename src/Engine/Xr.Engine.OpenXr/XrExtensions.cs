#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using OpenXr.Engine;
using OpenXr.Engine.OpenGL;
using OpenXr.Engine.OpenGL.Oculus;
using Silk.NET.OpenXR;
using OpenXr.Framework;


namespace Xr.Engine.OpenXr
{
    public unsafe static class XrExtensions
    {
        public delegate IGlRenderTarget GlRenderTargetFactory(GL gl, uint texId);

        public static GetQuadDelegate BindToQuad(this TriangleMesh mesh)
        {
            return () =>
            {
                var result = new XrQuad
                {
                    IsVisible = mesh.IsVisible && mesh.Parent != null,
                    Size = new System.Numerics.Vector2(mesh.Transform.Scale.X, mesh.Transform.Scale.Y),
                    Orientation = mesh.Transform.Orientation,
                    Position = mesh.Transform.Position
                };

                return result;
            };
        }

        public static OpenGLRender BindEngineApp(this XrApp xrApp, EngineApp app, uint sampleCount = 1, bool multiView = false)
        {
            GlRenderTargetFactory factory;

            if (multiView)
                factory = (gl, texId) => GlMultiViewRenderTarget.Attach(gl, texId, sampleCount);
            else
            {
                if (sampleCount == 1)
                    factory = (gl, texId) => GlTextureRenderTarget.Attach(gl, texId, sampleCount);
                else
                    factory = (gl, texId) =>
                    {
                        var target = gl.GetTexture2DTarget(texId);
                        if (target == TextureTarget.Texture2DMultisample)
                            return GlTextureRenderTarget.Attach(gl, texId, sampleCount);
                        else
                            return GlMultiSampleRenderTarget.Attach(gl, texId, sampleCount);
                    };
            }

            return xrApp.BindEngineApp(app, factory, multiView);
        }

        public static OpenGLRender BindEngineApp(this XrApp xrApp, EngineApp app, GlRenderTargetFactory targetFactory, bool multiView)
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
                renderer.EnableDebug();
                app.Renderer = renderer;
            }
            else
                renderer = (OpenGLRender)app.Renderer;

            app.Start();

            void RenderView(ref CompositionLayerProjectionView view, SwapchainImageBaseHeader* image, int viewIndex, long predTime)
            {
                var glImage = (SwapchainImageOpenGLKHR*)image;

                var rect = view.SubImage.ImageRect.Convert().To<Rect2I>();

                var renderTarget = targetFactory(renderer.GL, glImage->Image);

                renderer.SetRenderTarget(renderTarget);

                var camera = (PerspectiveCamera)app.ActiveScene!.ActiveCamera!;

                var transform = XrCameraTransform.FromView(view, camera.Near, camera.Far);

                camera.Projection = transform.Projection;
                camera.View = transform.View;

                if (viewIndex == 0)
                    app.RenderFrame(rect);
                else
                    app.Renderer.Render(app.ActiveScene, camera, rect);

            }

            void RenderMultiView(ref Span<CompositionLayerProjectionView> views, SwapchainImageBaseHeader* image, long predTime)
            {
                var glImage = (SwapchainImageOpenGLKHR*)image;

                var rect = views[0].SubImage.ImageRect.Convert().To<Rect2I>();

                var renderTarget = targetFactory(renderer.GL, glImage->Image);

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

            app.ActiveScene!.AddChild(new XrGroup(xrApp));

            return renderer;
        }
    }
}
