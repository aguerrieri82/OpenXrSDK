using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using XrEngine;
using XrEngine.Audio;
using XrEngine.OpenXr;
using XrMath;

namespace XrSamples.Graffiti
{
    public class MainScene : Scene3D
    {
        private Can _can;

        public MainScene()
        {
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

            _can = new Can();

            AddChild(_can);
        }

        public void Configure(XrEngineApp e)
        {
            _can.Configure(e);
        }
    }
}
