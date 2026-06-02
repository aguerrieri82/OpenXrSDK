using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using XrEngine;
using XrEngine.Audio;
using XrEngine.OpenGL;
using XrEngine.OpenXr;
using XrMath;

namespace XrSamples.Graffiti
{


    public class MainScene : Scene3D
    {
        private Can _can;
        private PaintCanvas _canvas;
        private SprayBrush _spray;
        private CanvasDrawer _canvasDrawer;
        private InputController _input;
        private PaintSelector _paintSelector;

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
                    Position= new Vector3(0, 1, 0),
                    Orientation = Quaternion.Identity
                },
                Size = new Vector2(2, 2)
            }, 0.0010f);

            _spray = new SprayBrush(30, 10);

            AddChild(_can);
            AddChild(_canvas);
            AddChild(_spray);

            _can.SetWorldPose(new Pose3()
            {
                Position = new Vector3(0f, 0.45999998f, 0.45999998f),
                Orientation = new Quaternion(0f, 0.551937f, 0f, 0.8338858f)
            });

            _paintSelector = AddChild(new PaintSelector());
        }

        public void Configure(XrEngineApp e)
        {
            _can.Configure(e);
            _input.Configure(e);
            _canvasDrawer.Configure(e);

            if (e.App.Renderer is OpenGLRender openGLRender)
                openGLRender.AddPass(new GlSimulationPass(openGLRender), 0);
        }
    }
}
