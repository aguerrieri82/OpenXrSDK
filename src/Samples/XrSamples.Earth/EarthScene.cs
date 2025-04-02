using XrEngine;
using XrMath;
using static XrSamples.Earth.SceneConst;

namespace XrSamples.Earth
{
    public class EarthScene : Scene3D
    {
        private readonly PerspectiveCamera _camera;
        private readonly PointLight _sunLight;
        private readonly Earth _earth;
        private readonly Sun _sun;
        private readonly Moon _moon;

        public EarthScene()
        {

            ModuleManager.Ref<TiffReader>();

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
            AddChild(_earth.CreateOrbit("#0000ff"));

            _moon = AddChild(new Moon());
            AddChild(_moon.CreateOrbit(Color.White));

            _sun = AddChild(new Sun());

            _earth.AddTile("output_SRTMGL1.tif",
                          "viz.SRTMGL1_roughness.png",
                          "2024-11-23-00_00_2024-11-23-23_59_Sentinel-2_L1C_True_color.jpg");

            AddChild(new StarDome());

            this.AddComponent<CameraControl>();

            ActiveCamera = _camera;
        }


        public Earth Earth => _earth;

        public Moon Moon => _moon;

        public Sun Sun => _sun;
    }
}
