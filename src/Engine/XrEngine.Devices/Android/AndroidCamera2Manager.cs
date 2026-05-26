
#if ANDROID29_0_OR_GREATER

using Android.Hardware.Camera2;
using Java.Lang;
using System.Runtime.Versioning;
using ContextA = global::Android.Content.Context;

namespace XrEngine.Devices.Android
{

    [SupportedOSPlatform("android29.0")]
    public class AndroidCamera2Manager : ICameraManager
    {
        const string KEY_CAMERA_POSITION = "com.meta.extra_metadata.position";
        const string KEY_CAMERA_SOURCE = "com.meta.extra_metadata.camera_source";

        private readonly CameraManager _manager;

        public AndroidCamera2Manager()
        {
            _manager = (CameraManager)Application.Context.GetSystemService(ContextA.CameraService)!;
        }

        public async Task<ICameraDevice> OpenCameraAsync(string id)
        {
            var camera = new AndroidCamera2(id, _manager);

            await camera.OpenAsync();

            return camera;
        }

        public IList<CameraDeviceInfo> GetCameras()
        {
            var result = new List<CameraDeviceInfo>();

            var idList = _manager.GetCameraIdList();

            foreach (var id in idList)
            {
                var device = new CameraDeviceInfo()
                {
                    Id = id
                };

                var chars = _manager.GetCameraCharacteristics(id);

                var javaType = Class.FromType(typeof(Integer));

                var facing = chars.Get(CameraCharacteristics.LensFacing);
                var source = chars.Get(new CameraCharacteristics.Key(KEY_CAMERA_SOURCE, javaType));
                var pos = chars.Get(new CameraCharacteristics.Key(KEY_CAMERA_POSITION, javaType));

                string? direction;
                string? posName;
                string? sourceName;

                if (facing != null)
                {
                    device.Facing = (int)facing;

                    direction = (LensFacing)device.Facing switch
                    {
                        LensFacing.Front => "Front",
                        LensFacing.Back => "Back",
                        LensFacing.External => "External",
                        _ => "Unknown"
                    };
                }

                if (pos != null)
                {
                    device.Position = (int)pos;

                    posName = device.Position switch
                    {
                        0 => "Left",
                        1 => "Right",
                        _ => "Unknown"
                    };
                }

                if (source != null)
                {
                    device.Source = (int)source;

                    if (device.Source == 0)
                        sourceName = "Passthrough";
                    else
                        sourceName = "Source " + device.Source;
                }

                result.Add(device);

            }

            return result;

        }
    }
}
#endif