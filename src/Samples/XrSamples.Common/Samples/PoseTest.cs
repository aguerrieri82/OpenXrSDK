using System.Numerics;
using XrEngine;
using XrEngine.OpenXr;
using XrMath;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        public static XrEngineAppBuilder CreatePoseTest(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var left = new Pose3
            {
                Position = new Vector3(-0.0320870019f, -0.0172204766f, -0.0633444414f),
                Orientation = new Quaternion(-0.995334148f, -0.00154464366f, -0.00174995663f, 0.0964596644f)
            };

            var right = new Pose3
            {
                Position = new Vector3(0.0315504968f, -0.017489884f, -0.0631345809f),
                Orientation = new Quaternion(-0.995401025f, 0.00226922776f, 0.00283159781f, 0.0957266465f)
            };

            Pose3 GetLensPose(Pose3 curPose)
            {
                var realPos = curPose.Position;
                realPos.X = -realPos.X;

                var rawRot = curPose.Orientation;

                var rot = rawRot;
                rot.Y = -rot.Y;
                rot.Z = -rot.Z;
                rot = Quaternion.Normalize(rot);

                var worldRot = Quaternion.Inverse(rot);
                var sensorFix = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI);

                return new Pose3
                {
                    Position = realPos,
                    Orientation = Quaternion.Normalize(worldRot * sensorFix)
                };
            }

            var headeset = new Group3D() { Name = "Headset" };

            headeset.AddChild(new PoseView(left, "Left", "#ff00ff"));
            headeset.AddChild(new PoseView(right, "Right", "#ffff00"));
            headeset.AddChild(new PoseView(new Pose3(), "Origin", "#ffffff"));

            scene.AddChild(headeset);

            var headeset2 = new Group3D() { Name = "Headset2" };

            headeset2.AddChild(new PoseView(GetLensPose(left), "Left", "#ff00ff"));
            headeset2.AddChild(new PoseView(GetLensPose(right), "Right", "#ffff00"));
            headeset2.AddChild(new PoseView(new Pose3(), "Origin", "#ffffff"));

            scene.AddChild(headeset2);

            scene.AddChild(headeset);

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .ConfigureSampleApp();
        }
    }
}
