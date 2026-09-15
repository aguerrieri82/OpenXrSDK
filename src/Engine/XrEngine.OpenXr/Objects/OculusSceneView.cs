
namespace XrEngine.OpenXr
{
    public class OculusSceneView : Group3D, OculusSceneLoader.INotifySceneLoaded
    {
        private readonly OculusSceneLoader _loader;

        public OculusSceneView()
            : this(DefaultSceneModelFactory.Instance)
        {
        }

        public OculusSceneView(ISceneModelFactory factory)
        {
            _loader = AddComponent(new OculusSceneLoader(factory));
        }

        public Object3D? AddChild(SceneModelInfo model)
        {
            var obj = _loader.Factory.CreateModel(model);
            if (obj != null)
                AddChild(obj);
            return obj;
        }

        public void NotifySceneLoaded()
        {
            SceneReady?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? SceneReady;

        public OculusSceneLoader Loader => _loader;

    }
}
