using XrEngine;
using XrInteraction;

namespace XrEditor
{

    public abstract class BaseKeyboardTool : IEditorTool
    {
        protected SceneView? _sceneView;
        protected bool _isActive;

        public BaseKeyboardTool()
        {
            _isActive = true;
        }

        public virtual void Attach(SceneView view)
        {
            _sceneView = view;
            _sceneView.RenderSurface.KeyUp += OnKeyUp;
            _sceneView.RenderSurface.KeyDown += OnKeyDown;

        }

        protected virtual void OnKeyUp(KeyboardEvent ev)
        {

        }

        protected virtual void OnKeyDown(KeyboardEvent ev)
        {

        }

        public virtual void NotifySceneChanged()
        {

        }

        protected DispatcherSwitch UiThread => Context.Require<IMainDispatcher>().Switch;

        public bool IsActive => _isActive;
    }
}
