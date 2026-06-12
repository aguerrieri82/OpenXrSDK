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

        public MainScene(bool reconstructMode)
        {
            ReconstructMode = reconstructMode;

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
            this.AddComponent<CanvasRecorder>();

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

            _can = new Can(reconstructMode);

            _canvas = new PaintCanvas(new Quad3
            {
                Pose = new Pose3
                {
                    Position = new Vector3(0, 1, 0),
                    Orientation = Quaternion.Identity
                },
                Size = new Vector2(2, 2)
            }, 0.0010f, !reconstructMode);

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

            /*
            var geo = new BrickGeometry();

            AddChild(new TriangleMesh(geo, new BrickMaterial
            {
                //Color = Color.Parse("#ff0000"),
            })
            {
                Name = "Wall"
            });

            var data = geo.BuildDensityTexture(0.001f, -0.01f);

            var tileSize = geo.DensityTileSize;
            var wallMin = -geo.WallSize * 0.5f;

            var scale = new Vector2(
                geo.WallSize.X / tileSize.X,
                geo.WallSize.Y / tileSize.Y);

            var translate = new Vector2(
                (wallMin.X - geo.Offset.X) / tileSize.X,
                (wallMin.Y - geo.Offset.Y) / tileSize.Y);



            var texture = Texture2D.FromData([data]);
            texture.Format = TextureFormat.GrayFloat16;
            texture.WrapT = WrapMode.Repeat;
            texture.WrapS = WrapMode.Repeat;
            texture.Transform = texture.Transform =
                 Matrix3x3.CreateTranslation(translate.X, translate.Y) *
                        Matrix3x3.CreateScale(scale.X, scale.Y);

            AddChild(new TriangleMesh(new Quad3D(new Vector2(2,2)), new TextureMaterial
            {
                Texture = texture

            })
            {
                Name = "WallImage"
            });
            */
        }


        [Action]
        public async Task Reproduce()
        {
            var record = CanvasRecordingReader.ReadFile("D:\\Projects\\XrEditor\\Graffiti\\Recording\\Graffiti-20260608-130312.json");

            _ = App!.Renderer!.Dispatcher.ExecuteAsync(() =>
            {

                var generator = new CanvasImageGenerator();
                using var image = generator.Generate(record, 0.001f / 6f);

                Log.Debug(this, "Encoding image...");

                using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                using var outStream = File.OpenWrite("d:\\image.png");
                data.SaveTo(outStream);

                Log.Debug(this, "Image saved");

            });
        }

        public GraffitiTool ActiveTool { get; set; }

        public bool ReconstructMode { get; set; }

        public void Configure(XrEngineApp e)
        {
            _can.Configure(e);
            _input.Configure(e);
            _canvasDrawer.Configure(e);

            if (!ReconstructMode)
            {
                this.Descendants<ImageLight>().First().Intensity = 0.8f;

                if (e.App.Renderer is OpenGLRender openGLRender)
                    openGLRender.AddPass(new GlSimulationPass(openGLRender, false), 0);
            }
        }
    }
}
