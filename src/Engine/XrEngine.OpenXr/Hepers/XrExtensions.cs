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
using OpenXr.Framework.Angle;
using StructureType = Silk.NET.OpenXR.StructureType;
using System.Reflection.Metadata.Ecma335;

namespace XrEngine.OpenXr
{
    public unsafe static class XrExtensions
    {

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

        public static IRenderEngine BindEngineApp(this XrApp xrApp, EngineApp app, XrEngineAppOptions options)
        {
            if (app.Renderer is OpenGLRender openGl)
                return xrApp.BindEngineAppGL(app, options.ProjDepthMode, options.Driver == GraphicDriver.Angle);

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

            void RenderView(ref RenderViewsInfo info)
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
                        format = ((GLEnum)xrApp.RenderOptions.ColorFormat) switch
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
                        format = ((Silk.NET.Vulkan.Format)xrApp.RenderOptions.ColorFormat) switch
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
                        camera.WorldMatrix = transform.World;
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

                        camera.Eyes[i].World = transform.World;
                        camera.Eyes[i].Projection = transform.Projection;

                        var depth = (CompositionLayerDepthInfoKHR*)info.ProjViews[i].Next;
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

            var projLayer = xrApp.Layers.AddProjection(RenderView, xrApp.RenderOptions.UseProjectionDepth);


            return renderer;
        }

        public static OpenGLRender BindEngineAppGL(this XrApp xrApp, EngineApp app, XrProjDepthMode depthMode, bool useAngle)
        {
            return new XrGlRenderBinding(xrApp, app, depthMode, useAngle).Renderer;
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
