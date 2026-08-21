using System;
using System.Collections.Generic;
using System.Text;
using XrEngine.Devices;

namespace XrEngine.Devices
{ 
    public class CameraRefractionSource : BaseComponent<Scene3D>, IScreenRefractionSource
    {
        private CameraController? _controller;

        public CameraRefractionSource(string leftMainId, string? rightId = null)
        {
            LeftMainId = leftMainId;
            RightId = rightId;
        }

        public Texture2D?[] GetRefractionTextures(PerspectiveCamera camera)
        {
            _controller ??= _host.Component<CameraController>();

            var result = new Texture2D?[camera.Eyes!.Length];

            result[0] = _controller.GetCameraStatus(LeftMainId).Texture;
            result[0]?.Transform = _controller.GetUvTransform(LeftMainId, camera.Eyes[0].Projection);

            if (result.Length > 1 && !string.IsNullOrWhiteSpace(RightId))
            {
                result[1] = _controller.GetCameraStatus(RightId).Texture;
                result[1]?.Transform = _controller.GetUvTransform(RightId, camera.Eyes[1].Projection);
            }

            return result;
        }

        public int Priority => -1;

        public string LeftMainId { get; }
        
        public string? RightId { get; }

    }
}
