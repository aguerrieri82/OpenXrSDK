using OpenXr.Engine;
using OpenXr.Engine.OpenGL;
using OpenXr.Framework.OpenGL;
using Silk.NET.OpenXR;
using System.Numerics;

namespace OpenXr.Framework
{
    public unsafe static class XrAppExtensions
    {
        public static void BindEngineApp(this XrApp xrApp, EngineApp app)
        {
            var driver = xrApp.Plugin<IXrGraphicDriver>();

            if (driver is XrOpenGLGraphicDriver glDriver)
            {
                var renderer = new OpenGLRender(glDriver.Device.View.GLContext!, glDriver.Device.Gl);
                renderer.EnableDebug();
                app.Renderer = renderer;

                app.Start();

                void RenderScene(ref CompositionLayerProjectionView view, SwapchainImageBaseHeader* image)
                {
                    var glImage = (SwapchainImageOpenGLKHR*)image;
                    var rect = view.SubImage.ImageRect.Convert().To<RectI>();

                    renderer.SetImageTarget(glImage->Image);

                    var camera = (PerspectiveCamera)app.ActiveScene!.ActiveCamera!;
                    
                    camera.SetFovCenter(view.Fov.AngleLeft, view.Fov.AngleRight, view.Fov.AngleUp, view.Fov.AngleDown);

                    camera.Transform.Position = view.Pose.Position.Convert().To<Vector3>();
                    camera.Transform.Orientation = view.Pose.Orientation.Convert().To<Quaternion>();

                    app.RenderFrame(rect);

                    glDriver.Device.View.DoEvents();
                }

                xrApp.Layers.AddProjection(RenderScene);

                return;
            }

            throw new NotSupportedException();
            
        }
    }
}
