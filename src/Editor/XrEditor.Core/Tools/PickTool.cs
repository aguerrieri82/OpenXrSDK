using XrEngine;
using XrEngine.Interaction;
using XrMath;

namespace XrEditor
{
    public abstract class PickTool : BasePointerTool, IDrawGizmos, IRayPointer
    {
        protected Object3D? _currentPick;
        protected Color? _oldColor;
        protected Collision? _lastCollision;
        protected RayPointerStatus _lastRay;

        public override void NotifySceneChanged()
        {
            var debug = _sceneView?.Scene?.Components<DebugGizmos>().FirstOrDefault();

            if (debug != null)
                debug.Debuggers.Add(this);
        }

        RayPointerStatus IRayPointer.GetPointerStatus()
        {
            var result = _lastRay;
            _lastRay.IsActive = false;
            return result;
        }

        protected IDispatcher EngineDispatcher => _sceneView.Scene.App.Renderer.Dispatcher;

        protected void UpdateRay(Pointer2Event ev)
        {
            _lastRay.Ray = ToRay(ev);
            _lastRay.Buttons = ev.Buttons;
            _lastRay.IsActive = true;
        }

        protected override void OnPointerDown(Pointer2Event ev)
        {
            UpdateRay(ev);
            base.OnPointerDown(ev);
        }

        protected override void OnPointerUp(Pointer2Event ev)
        {
            UpdateRay(ev);
            base.OnPointerUp(ev);
        }

        protected override async void OnPointerMove(Pointer2Event ev)
        {
            if (_sceneView?.Scene == null)
                return;

            UpdateRay(ev);

            _lastCollision = (await EngineDispatcher.ExecuteAsync(() => _sceneView.Scene.RayCollisions(_lastRay.Ray)))
                           .OrderBy(a => a.Distance)
                           .FirstOrDefault();


            var newPick = _lastCollision?.Object;

            if (newPick != null && !CanPick(newPick))
                newPick = null;

            if (_currentPick == newPick)
                return;

            if (_currentPick != null)
                OnLeave(_currentPick);

            _currentPick = newPick;

            if (_currentPick != null)
                OnEnter(_currentPick);
        }

        protected virtual bool CanPick(Object3D obj)
        {
            return true;
        }

        protected virtual void OnLeave(Object3D obj)
        {
            if (obj is TriangleMesh mesh && mesh.Materials.Count > 0 && mesh.Materials[0] is BasicMaterial mat && _oldColor != null)
            {
                mat.Color = _oldColor.Value;
                mat.NotifyChanged(ObjectChangeType.Render);
            }

        }

        protected virtual void OnEnter(Object3D obj)
        {
            if (obj is TriangleMesh mesh && mesh.Materials.Count > 0 && mesh.Materials[0] is BasicMaterial mat)
            {
                _oldColor = mat.Color;
                mat.Color = new Color(0, 1, 0, 1);
                mat.NotifyChanged(ObjectChangeType.Render);
            }
        }

        public virtual void DrawGizmos(Canvas3D canvas)
        {

        }

        public void CapturePointer()
        {
            if (_sceneView != null)
                _sceneView.ActiveTool = this;
        }

        public void ReleasePointer()
        {
            if (_sceneView?.ActiveTool == this)
                _sceneView.ActiveTool = null;
        }

        int IRayPointer.PointerId => -100;

    }
}
