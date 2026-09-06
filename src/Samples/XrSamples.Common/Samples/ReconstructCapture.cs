using System.Numerics;
using System.Xml.Linq;
using XrEngine;
using XrEngine.Devices;
using XrEngine.OpenXr;
using XrEngine.Reconstruct;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Reconstruct Capture")]
        public static XrEngineAppBuilder CreateReconstructCapture(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var recorder = new XrReconstructRecorder();

            CameraParams cameraParams = new();

            var display = new TriangleMesh(Quad3D.Default, new EyeTextureMaterial(recorder.LeftTex, recorder.RightTex));

            display.Name = "capture_display";
            display.Transform.Scale = new Vector3(1.08f, 1.08f, 0.01f);
            display.AddComponent<MeshCollider>();

            scene.AddChild(display);

            var cameraState = 0;
            var startTime = DateTime.Now;
            var isRoomCaptured = false;
            var sharedPath = Context.Require<IPlatform>().SharedPath;

            scene.AddBehavior((_, ctx) =>
            {
                if (cameraState == 0 && recorder.LeftTex.Handle != 0)
                {
                    cameraState = 1;

                    recorder.StartCaptureAsync(Context.Require<IPlatform>().SharedPath!).ContinueWith(a =>
                    {
                        cameraState = 2;
                    });
                }

                if (!isRoomCaptured)
                {
                    var model = scene.FindByName<TriangleMesh>("Mesh");

                    if (model != null && model.Component<XrAnchorUpdate>().HasPose)
                    {
                        model.Geometry!.EnsureIndices();
                        var writer = new ObjWriter();
                        writer.Add(model);
                        File.WriteAllText(Path.Combine(sharedPath, "scene.obj"), writer.Text());
                        recorder.Stats!.ScenePosition = model.GetWorldPose();
                        model.Remove();
                        isRoomCaptured = true;
                    }

                }

                if (cameraState == 2)
                {
                    try
                    {
                        recorder.CaptureFrame(scene.ActiveCamera!);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("CreateReconstructCapture", ex, "CaptureFrame");
                    }

                    recorder.UpdateTextures();

                    if ((DateTime.Now - startTime).TotalSeconds >= 30)
                    {
                        recorder.StopCapture();
                        cameraState = 3;
                    }
                }
            });

            return builder
                .UseApp(app)
                .UseClickMoveFront(display, 0.5f)
                .UseEnvironmentDepth()
                .UseSceneMesh(true, false)
                .ConfigureSampleApp();
        }
    }
}
