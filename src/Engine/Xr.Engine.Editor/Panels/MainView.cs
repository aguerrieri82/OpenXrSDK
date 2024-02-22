using OpenXr.Engine;
using OpenXr.Samples;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Xr.Engine.OpenXr;


namespace Xr.Engine.Editor
{
    public class MainView : BaseView
    {

        private EngineApp _app;

        public MainView()
        {
            SceneView = new SceneView();
            PropertiesEditor = new PropertiesEditor();

            LoadScene();
        }


        [MemberNotNull(nameof(_app))]
        public void LoadScene()
        {
            _app = SampleScenes.CreateSimpleScene(new LocalAssetManager("Assets"));

            var cube = _app.ActiveScene!.FindByName<Object3D>("mesh");
            if (cube != null)
            {
               // cube.AddComponent<BoundsGrabbable>();
                PropertiesEditor.ActiveObject = cube;
            }

            var quad = _app.ActiveScene!.FindByName<Object3D>("quad");
            if (quad != null && false)
                quad.AddComponent(new FollowCamera { Offset = new Vector3(0, 0, -2) });

            _app.Start();

            SceneView.Scene = _app.ActiveScene;
        }

        public SceneView SceneView { get; }

        public PropertiesEditor PropertiesEditor { get; }
    }
}
