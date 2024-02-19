using OpenXr.Engine;
using OpenXr.Samples;
using System.Diagnostics.CodeAnalysis;
using Xr.Engine.OpenXr;


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
            _app = SampleScenes.CreateSimpleScene(new LocalAssetManager("Assets"));
            _cube = _app.ActiveScene!.Descendants<Mesh>().First();
            _cube.AddComponent<BoundsGrabbable>();  
            _app.Start();
        }

        public SceneView SceneView { get; }

        public PropertiesEditor PropertiesEditor { get; }
    }
}
