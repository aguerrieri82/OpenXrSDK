
using OpenXr.Engine;
using OpenXr.Engine.OpenGL;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;


namespace Xr.Engine.Editor
{
    public class MainView : BaseView
    {
        private Mesh _cube;
        private EngineApp _app;

        public MainView()
        {
            LoadScene();
            SceneView = new SceneView();
            PropertiesEditor = new PropertiesEditor();

            SceneView.Scene = _app.ActiveScene;
            PropertiesEditor.ActiveObject = _cube;
        }

        [MemberNotNull(nameof(_cube))]
        [MemberNotNull(nameof(_app))]
        public void LoadScene()
        {
            _app = new EngineApp();

            var scene = new Scene();

            _cube = new Mesh(Cube.Instance, new StandardMaterial() { Color = new Color(1f, 0, 0, 1) });
            _cube.Transform.Pivot = new Vector3(0, -1, 0);
            _cube.Transform.SetScale(0.1f);
            _cube.Transform.SetPositionX(0.5f);

            scene.AddChild(_cube);

            scene.AddChild(new AmbientLight(0.3f));
            scene.AddChild(new PointLight()).Transform.Position = new Vector3(0, 10, 10);

            scene.AddChild(new PlaneGrid(6f, 12f, 2f));

            var camera = new PerspectiveCamera() { Far = 50f };
            camera.BackgroundColor = Color.White;   
            camera!.LookAt(new Vector3(2f, 2f, 2f), Vector3.Zero, new Vector3(0, 1, 0));

            scene.ActiveCamera = camera;

            _app.OpenScene(scene);

            _app.Start();
        }


        public SceneView SceneView { get; }

        public PropertiesEditor PropertiesEditor { get; }
    }
}
