using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using System.Numerics;
using XrEngine;
using XrEngine.Audio;
using XrEngine.OpenGL;
using XrEngine.OpenXr;
using XrMath;
using XrSamples.Graffiti.Objects;

namespace XrSamples.Graffiti
{
    public enum GraffitiTool
    {
        None,
        CanvasDraw,
        PaintSelector
    }

    public class MainScene : Scene3D
    {
        private readonly Can _can;
        private readonly PaintCanvas _canvas;
        private readonly SprayBrush _spray;
        private readonly SprayRays _sprayRays;
        private readonly CanvasDrawer _canvasDrawer;
        private readonly InputController _input;
        private readonly PaintSelector _paintSelector;

        public MainScene()
        {
            if (XrPlatform.IsEditor)
                AddChild(new PlaneGrid(6f, 12f, 2f));

            var camera = new PerspectiveCamera
            {
                Far = 100f,
                Near = 0.01f,
                BackgroundColor = new Color(0, 0, 0, 0),
                Exposure = 1
            };

            camera.LookAt(new Vector3(1, 1.7f, 1), new Vector3(0, 0, 0), new Vector3(0, 1, 0));

            ActiveCamera = camera;

            this.AddComponent<AudioSystem>();
            this.AddComponent<DebugGizmos>();
            this.AddComponent<SpatialAnchorGrid>();

            _canvasDrawer = this.AddComponent<CanvasDrawer>();

            _input = this.AddComponent<InputController>();

            if (XrPlatform.IsEditor)
            {
                this.AddComponent<XrInputRecorder>();
                AddComponent(new XrInputPlayer
                {
                    UseReferenceTime = true,
                    RealTime = true
                });
            }

            _can = new Can();

            _canvas = new PaintCanvas(new Quad3
            {
                Pose = new Pose3
                {
                    Position = new Vector3(0, 1, 0),
                    Orientation = Quaternion.Identity
                },
                Size = new Vector2(2, 2)
            }, 0.0010f);

            _spray = new SprayBrush(30, 10);

            _sprayRays = new SprayRays(_spray.Geometry!);

            AddChild(_can);
            AddChild(_canvas);
            AddChild(_spray);
            AddChild(_sprayRays);

            _can.SetWorldPose(new Pose3()
            {
                Position = new Vector3(0f, 0.45999998f, 0.45999998f),
                Orientation = new Quaternion(0f, 0.551937f, 0f, 0.8338858f)
            });

            _paintSelector = AddChild(new PaintSelector());
        }


        [Action]
        public async Task DiscoverSpaces()
        {
            var meta = XrApp.Current!.Plugin<OculusXrPlugin>();

            var spaces = await meta.DiscoverSpacesAsync();

            Console.WriteLine(spaces);
        }

        public GraffitiTool ActiveTool { get;  set; }

        public void Configure(XrEngineApp e)
        {
            _can.Configure(e);
            _input.Configure(e);
            _canvasDrawer.Configure(e);

            this.Descendants<ImageLight>().First().Intensity = 0.8f;

            if (e.App.Renderer is OpenGLRender openGLRender)
                openGLRender.AddPass(new GlSimulationPass(openGLRender), 0);
        }
    }
}
