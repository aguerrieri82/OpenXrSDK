using System;
using System.Collections.Generic;
using System.Text;
using XrEngine;
using XrEngine.Devices;
using XrEngine.Media;
using XrEngine.OpenGL;

namespace XrSamples.Components
{
    public class CameraCapture : AsyncBehavior<Scene3D>
    {
        private ICameraDevice? _camera;
        private ICameraManager? _manager;

        public CameraCapture()
        {
            Texture = new Texture2D
            {
                Format = TextureFormat.Rgba32,
                WrapT = WrapMode.ClampToEdge,
                WrapS = WrapMode.ClampToEdge,
                MagFilter = ScaleFilter.Linear,
                MinFilter = ScaleFilter.Linear,
                Type = TextureType.External
            };
        }

        public CameraCapture(Texture2D texture)
        {
            Texture = texture;
        }

        protected override void Start(RenderContext ctx)
        {
            _manager = Context.Require<ILocalCameraManger>();
            base.Start(ctx);
        }

        protected override async Task UpdateAsync()
        {
            if (_camera == null)
            {
                var cameras = _manager!.GetCameras();

                if (cameras == null || cameras.Count == 0)
                    return;

                var infoLeft = cameras.First(a => a.Source == 0 && a.Position == 0);

                _camera = await _manager.OpenCameraAsync(infoLeft.Id!).ConfigureAwait(true);

                var formats = _camera.GetSupportedFormats();

                var curFormat = formats
                   .OrderByDescending(a => a.Width * a.Height)
                   .ThenByDescending(a => a.FrameRate)
                   .FirstOrDefault();

                Texture.Width = (uint)curFormat.Width;
                Texture.Height = (uint)curFormat.Height;

                await Dispatcher!.ExecuteAsync(() => Texture.ToGlTexture());

                await _camera.StartCaptureAsync(curFormat, Texture).ConfigureAwait(true);
            }
            else
                _camera.UpdateTexture();
        }

        public ICameraDevice? Camera => _camera;

        public Texture2D Texture { get; }
    }
}
