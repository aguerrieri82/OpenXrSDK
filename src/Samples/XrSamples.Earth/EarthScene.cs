﻿using CanvasUI;
using System.Numerics;
using XrEngine;
using XrEngine.OpenXr;
using XrMath;
using static XrSamples.Earth.SceneConst;

namespace XrSamples.Earth
{
    public class EarthScene : Scene3D
    {
        private PerspectiveCamera _camera;
        private PointLight _sunLight;
        private Earth _earth;
        private Sun _sun;

        public EarthScene()
        {

            AddChild(new PlaneGrid(6f, 12f, 2f));

            _camera = AddChild(new PerspectiveCamera
            {
                Far = AU * 2,
                Near = Unit(1f),
                BackgroundColor = new Color(0, 0, 0, 0),
                Exposure = 1
            });

            _sunLight = AddChild(new PointLight()
            {
                Name = "Sun Light",
                Intensity = 4,
                Range = AU * 2,
            });

            _earth = AddChild(new Earth());
            AddChild(_earth.CreateOrbit());

            _sun = AddChild(new Sun());

            _earth.AddTile("output_SRTMGL1.tif",
                          "viz.SRTMGL1_roughness.png",
                          "2024-11-23-00_00_2024-11-23-23_59_Sentinel-2_L1C_True_color.jpg");

            this.AddComponent<CameraControl>();

            ActiveCamera = _camera;
        }


        public Earth Earth => _earth;

        public Sun Sun => _sun;
    }
}
