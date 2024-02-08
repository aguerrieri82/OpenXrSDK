using OpenXr.Engine;
using OpenXr.Engine.OpenGL;
using OpenXr.Framework.OpenGL;
using Silk.NET.OpenXR;
using System.Numerics;

namespace OpenXr.Framework
{
    public static class XrAppExtensions
    {
        public static void BindEngineApp(this XrApp xrApp, EngineApp app)
        {
            var driver = xrApp.Plugin<IXrGraphicDriver>();

            if (driver is XrOpenGLGraphicDriver glDriver)
            {
                var renderer = new OpenGLRender(glDriver.Device.View.GLContext!, glDriver.Device.Gl);
                app.Renderer = renderer;

                app.Start();

                void RenderScene(ref CompositionLayerProjectionView view, NativeArray<SwapchainImageBaseHeader> images)
                {
                    var image = images.Item<SwapchainImageOpenGLKHR>((int)view.SubImage.ImageArrayIndex);

                    renderer.SetImageTarget(image.Image);

                    var camera = (PerspectiveCamera)app.ActiveScene!.ActiveCamera!;
                    
                    camera.SetFovCenter(view.Fov.AngleLeft, view.Fov.AngleRight, view.Fov.AngleUp, view.Fov.AngleDown);

                    camera.Transform.Position = view.Pose.Position.Convert().To<Vector3>();
                    camera.Transform.Orientation = view.Pose.Orientation.Convert().To<Quaternion>();

                    app.RenderFrame(view.SubImage.ImageRect.Convert().To<RectI>());
                }

                xrApp.Layers.AddProjection(RenderScene);
            }

            throw new NotSupportedException();
            
        }
    }
}
