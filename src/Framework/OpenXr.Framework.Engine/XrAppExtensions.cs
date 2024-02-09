using OpenXr.Engine;
using OpenXr.Engine.OpenGLES;
using OpenXr.Framework.Abstraction;
using OpenXr.Framework.OpenGLES;
using Silk.NET.Maths;
using Silk.NET.OpenGLES;
using Silk.NET.OpenXR;
using System.Numerics;


namespace OpenXr.Framework
{
    public unsafe static class XrAppExtensions
    {
        public static void BindEngineApp(this XrApp xrApp, EngineApp app)
        {
            var driver = xrApp.Plugin<IXrGraphicDriver>();

            if (driver is IApiProvider apiProvider)
            {
                var gl = apiProvider.GetApi<GL>();

                var renderer = new OpenGLESRender(gl);
                renderer.EnableDebug();
                app.Renderer = renderer;

                app.Start();

                void RenderScene(ref CompositionLayerProjectionView view, SwapchainImageBaseHeader* image, long predTime)
                {
                    var glImage = (SwapchainImageOpenGLKHR*)image;
                    var rect = view.SubImage.ImageRect.Convert().To<RectI>();

                    renderer.SetImageTarget(glImage->Image);

                    var camera = (PerspectiveCamera)app.ActiveScene!.ActiveCamera!;

                    camera.Projection = XrMath.CreateProjectionFov(
                        MathF.Tan(view.Fov.AngleLeft),
                        MathF.Tan(view.Fov.AngleRight),
                        MathF.Tan(view.Fov.AngleUp),
                        MathF.Tan(view.Fov.AngleDown),
                        camera.Near,
                        camera.Far);

                    var pose = view.Pose.ToXrPose();

                    var matrix = (Matrix4x4.CreateFromQuaternion(pose.Orientation) *
                                  Matrix4x4.CreateTranslation(pose.Position))
                                 .InvertRigidBody();

                    camera.Transform.SetMatrix(matrix);

                    app.RenderFrame(rect);

                    //glDriver.Device.View.DoEvents();
                }

                xrApp.Layers.AddProjection(RenderScene);

                return;
            }

            throw new NotSupportedException();

        }
    }
}
