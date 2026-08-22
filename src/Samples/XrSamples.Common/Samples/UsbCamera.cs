using System.Numerics;
using XrEngine;
using XrEngine.Devices;
using XrEngine.Media;
using XrEngine.OpenXr;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Usb Camera")]
        public static XrEngineAppBuilder CreateUsbCamera(this XrEngineAppBuilder builder)
        {
            var manager = Context.Require<IUsbCameraManger>();

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var texture = new Texture2D
            {
                Format = TextureFormat.Rgba8,
                WrapT = WrapMode.ClampToEdge,
                WrapS = WrapMode.ClampToEdge,
                MagFilter = ScaleFilter.Linear,
                MinFilter = ScaleFilter.Linear,
            };

            var main = new TriangleMesh(Quad3D.Default, new TextureMaterial(texture))
            {
                Name = "Usb"
            };

            main.Transform.Scale = new Vector3(1.08f, 1.08f, 0.01f);

            scene.AddChild(main);

            var cameraState = 0;

            ICameraDevice? camera = null;

            scene.AddBehavior(async (_, _) =>
            {
                var button = XrEngineApp.Current?.Inputs?.Right?.Button?.BClick;

                var aPressed = button != null && button.IsChanged && button.Value;

                if (cameraState == 0 && texture.Handle != 0)
                {
                    var cameras = manager!.GetCameras();

                    if (cameras == null || cameras.Count == 0)
                        return;

                    cameraState = 1;

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            camera = await manager.OpenCameraAsync(cameras[0].Id!);

                            var formats = camera.GetSupportedFormats();

                            var curFormat = formats
                               .Where(a => a.ImageFormat == ImageFormat.Rgb32)
                               .OrderByDescending(a => a.Width * a.Height)
                               .ThenByDescending(a => a.FrameRate)
                               .FirstOrDefault();

                            var ratio = (float)curFormat.Width / curFormat.Height;
                            var height = 0.5f;
                            var width = height * ratio;

                            await EngineApp.MainThread;

                            main.Transform.Scale = new Vector3(width, height, 0.01f);

                            await camera.StartCaptureAsync(curFormat, texture);

                            cameraState = 2;
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Usb", ex);
                        }

                    });
                }

                if (cameraState == 2)
                    camera?.UpdateTexture();
            });

            return builder
                .UseApp(app)
                .UseClickMoveFront(main)
                .ConfigureSampleApp();
        }
    }
}
