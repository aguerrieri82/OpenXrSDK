using CanvasUI;
using System;
using System.Collections.Generic;
using System.Text;
using XrEngine;
using XrEngine.Devices;
using XrEngine.Media;

#if __ANDROID__
using XrEngine.Android.Devices;
#endif


namespace XrSamples
{
    public class CameraTexturePlayer : AsyncBehavior<Object3D>
    {
        private ICameraDevice? _camera;
        private ICameraManager? _manager;

        public CameraTexturePlayer(Texture2D texture)
        {
            Texture = texture;  
        }

        protected override void Start(RenderContext ctx)
        {

#if __ANDROID__
            _manager = Context.Require<AndroidUsbCameraManager>();
#endif
            base.Start(ctx);
        }

        protected override async Task UpdateAsync()
        {
            try
            {
                if (_camera == null)
                {
                    var cameras = _manager!.GetCameras();

                    if (cameras == null || cameras.Count == 0)
                        return;

                    _camera = await _manager.OpenCameraAsync(cameras[0].Id!);

                    var formats = _camera.GetSupportedFormats();

                    var curFormat = formats
                       .Where(a => a.ImageFormat == ImageFormat.Rgb32)
                       .OrderByDescending(a => a.Width * a.Height)
                       .ThenByDescending(a => a.FrameRate)
                       .FirstOrDefault();

                    var ratio = (float)curFormat.Width / curFormat.Height;
                    var height = 0.5f;
                    var width = height * ratio;

                    _host!.Transform.SetScale(width, height, _host.Transform.Scale.Z);

                    await _camera.StartCaptureAsync(curFormat, Texture);
                }

                if (_camera != null)
                    _camera.UpdateTexture();

            }
            catch (Exception ex)
            {
                Log.Error("Usb", ex);
            }
        }

        public Texture2D Texture  { get; }
    }
}
