using OpenXr.Framework;
using System.Diagnostics;
using System.Numerics;
using XrEngine;
using XrEngine.Devices;
using XrEngine.OpenXr;
using XrMath;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Capture")]
        public static XrEngineAppBuilder CreateCapture(this XrEngineAppBuilder builder)
        {
            #region HELPERS

            static Rect2 CalcSensorCropRegion(
                float sensorWidth,
                float sensorHeight,
                float currentWidth,
                float currentHeight)
            {
                var scaleX = currentWidth / sensorWidth;
                var scaleY = currentHeight / sensorHeight;

                var maxScale = MathF.Max(scaleX, scaleY);

                scaleX /= maxScale;
                scaleY /= maxScale;

                return new Rect2
                {
                    X = sensorWidth * (1.0f - scaleX) * 0.5f,
                    Y = sensorHeight * (1.0f - scaleY) * 0.5f,
                    Width = sensorWidth * scaleX,
                    Height = sensorHeight * scaleY
                };
            }

            static Matrix4x4 ComputeQuadMatrixV2(
                Matrix4x4 headMatrix,
                CameraParams cam,
                float distanceMeters)
            {

                var fx = cam.Fx;
                var fy = cam.Fy;
                var cx = cam.Cx;
                var cy = cam.Cy;

                var sensorW = cam.SensorSize!.Value.Width;
                var sensorH = cam.SensorSize.Value.Height;

                var currentW = cam.CurrentSize.Width;
                var currentH = cam.CurrentSize.Height;

                var crop = CalcSensorCropRegion(
                    sensorW,
                    sensorH,
                    currentW,
                    currentH);

                var x0 = distanceMeters * ((crop.X - cx) / fx);
                var x1 = distanceMeters * ((crop.X + crop.Width - cx) / fx);

                var y0 = distanceMeters * ((crop.Y - cy) / fy);
                var y1 = distanceMeters * ((crop.Y + crop.Height - cy) / fy);

                var centerX = (x0 + x1) * 0.5f;
                var centerY = (y0 + y1) * 0.5f;

                var scaleX = x1 - x0;
                var scaleY = y1 - y0;

                var quadToSensor =
                    Matrix4x4.CreateScale(scaleX, scaleY, 1.0f) *
                    Matrix4x4.CreateTranslation(centerX, centerY, -distanceMeters);

                var sensorToHead = cam.GetLensPose().ToMatrix();

                return quadToSensor * sensorToHead * headMatrix;
            }

            static Matrix4x4 ComputeQuadMatrixScaledFrom1m(
                Matrix4x4 headMatrix,
                Matrix4x4 eyeMatrix,
                CameraParams cam,
                float distanceMeters)
            {
                const float referenceDistance = 3.0f;

                var quadAt1m =
                    ComputeQuadMatrixV2(headMatrix, cam, referenceDistance);

                var scale =
                    distanceMeters / referenceDistance;

                var eyePos = eyeMatrix.Translation;

                var aroundEye =
                    Matrix4x4.CreateTranslation(-eyePos) *
                    Matrix4x4.CreateScale(scale) *
                    Matrix4x4.CreateTranslation(eyePos);

                return quadAt1m * aroundEye;
            }

            #endregion

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            CameraParams leftParams = new();
            CameraParams rightParams = new();

            var leftTex = new Texture2D
            {
                Format = TextureFormat.Rgba8,
                WrapT = WrapMode.ClampToEdge,
                WrapS = WrapMode.ClampToEdge,
                MagFilter = ScaleFilter.Linear,
                MinFilter = ScaleFilter.Linear,
                Type = TextureType.External
            };

            var rightTex = new Texture2D
            {
                Format = TextureFormat.Rgba8,
                WrapT = WrapMode.ClampToEdge,
                WrapS = WrapMode.ClampToEdge,
                MagFilter = ScaleFilter.Linear,
                MinFilter = ScaleFilter.Linear,
                Type = TextureType.External
            };

            var mainLeft = new TriangleMesh(Quad3D.Default, new EyeTextureMaterial(leftTex, rightTex)
            {
                FixedEye = 0,
                UseDepth = false
            });

            mainLeft.Name = "MainLeft";
            mainLeft.Transform.Scale = new Vector3(0.7f, 0.7f, 0.01f);

            var right = new TriangleMesh(Quad3D.Default, new EyeTextureMaterial(leftTex, rightTex)
            {
                FixedEye = 1,
                UseDepth = false
            });

            right.Name = "Right";
            right.Transform.Scale = mainLeft.Transform.Scale;

            scene.AddChild(mainLeft);
            scene.AddChild(right);

            var cameraState = 0;

            var mustTrack = false;

            ICameraDevice? cameraLeft = null;
            ICameraDevice? cameraRight = null;

            var leftPos = Vector3.Zero;

            scene.AddBehavior(async (_, _) =>
            {
                var button = XrEngineApp.Current?.Inputs?.Right?.Button?.BClick;

                var aPressed = button != null && button.IsChanged && button.Value;

                if (aPressed)
                    mustTrack = !mustTrack;

                if (cameraState == 0)
                {
                    rightTex.Generate();
                    leftTex.Generate();

                    cameraState = 1;

                    _ = Task.Run(async () =>
                    {
                        var manager = Context.Require<ILocalCameraManger>();

                        var cameras = manager.GetCameras();

                        Log.Info("", "CAMERAS: {0}", string.Join(',', cameras.Select(a => a.Id)));

                        var infoLeft = cameras.First(a => a.Source == 0 && a.Position == 0);
                        var infoRight = cameras.First(a => a.Source == 0 && a.Position == 1);

                        cameraLeft = await manager.OpenCameraAsync(infoLeft.Id!);
                        cameraRight = await manager.OpenCameraAsync(infoRight.Id!);

                        var formats = cameraLeft.GetSupportedFormats();

                        var curFormat = formats.Last();

                        await Task.WhenAll(cameraLeft.StartCaptureAsync(curFormat, leftTex),
                                           cameraRight.StartCaptureAsync(curFormat, rightTex));

                        cameraState = 2;

                        leftParams = cameraLeft.GetParams();
                        rightParams = cameraRight.GetParams();

                    });
                }

                if (cameraState == 2)
                {
                    cameraLeft?.UpdateTexture();
                    cameraRight?.UpdateTexture();

                    if (cameraLeft!.LastTimestamp == 0 || cameraRight!.LastTimestamp == 0)
                        return;

                    var headLeftTime = XrApp.Current!.LocateSpace(XrApp.Current.Head,
                        XrApp.Current.ReferenceSpace, cameraLeft!.LastTimestamp).Pose;

                    var headRightTime = XrApp.Current!.LocateSpace(XrApp.Current.Head,
                        XrApp.Current.ReferenceSpace, cameraRight!.LastTimestamp).Pose;

                    if (mustTrack)
                    {
                        var thumb = XrEngineApp.Current?.Inputs?.Right?.Thumbstick!.Value;

                        Debug.Assert(scene.ActiveCamera?.Eyes != null);

                        mainLeft.WorldMatrix = ComputeQuadMatrixScaledFrom1m(headLeftTime.ToMatrix(), scene.ActiveCamera.Eyes[0].World, leftParams, 2f + (thumb!.Value.Y * 2f));
                        right.WorldMatrix = ComputeQuadMatrixScaledFrom1m(headRightTime.ToMatrix(), scene.ActiveCamera.Eyes[1].World, rightParams, 2f + (thumb!.Value.Y * 2f));

                    }
                }
            });

            return builder
                .UseApp(app)
                .ConfigureSampleApp();
        }
    }
}
