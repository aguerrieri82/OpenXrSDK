using Xr.Engine;

namespace Xr.Editor
{
    public class PickTool : BaseMouseTool
    {
        protected Object3D? _currentPick;
        protected Color? _oldColor;

        protected override void OnMouseMove(PointerEvent ev)
        {
            var ray = ToRay(ev);


            var collision = _sceneView!.Scene!.RayCollisions(ray).FirstOrDefault();

            var newPick = collision?.Object;

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
            if (obj is TriangleMesh mesh && mesh.Materials[0] is StandardMaterial mat && _oldColor != null)
            {
                mat.Color = _oldColor.Value;
                mat.NotifyChanged();
            }

        }

        protected virtual void OnEnter(Object3D obj)
        {
            if (obj is TriangleMesh mesh && mesh.Materials[0] is StandardMaterial mat)
            {
                _oldColor = mat.Color;
                mat.Color = new Color(0, 1, 0, 1);
                mat.NotifyChanged();
            }
        }
    }
}
