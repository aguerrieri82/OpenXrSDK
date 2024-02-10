namespace OpenXr.Engine
{
    public class Scene : Group
    {
        private Camera? _activeCamera;

        public Scene()
        {
        }

        public void Render(RenderContext ctx)
        {
            Update(ctx);

        }

        public Camera? ActiveCamera
        {
            get => _activeCamera;
            set
            {
                if (_activeCamera == value)
                    return;
                _activeCamera = value;

                if (_activeCamera != null && _activeCamera.Scene != this)
                    AddChild(_activeCamera);
            }
        }
    }
}
