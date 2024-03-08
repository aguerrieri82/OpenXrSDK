using System.Numerics;
using Xr.Engine;


namespace Xr.Editor
{
    [Flags]
    public enum MouseButton
    {
        Left = 0x1,
        Middle = 0x2,
        Right = 0x4
    }

    public abstract class BaseMouseTool : IEditorTool
    {
        protected SceneView? _sceneView;
        protected bool _isActive;

        public BaseMouseTool()
        {
            _isActive = true;
        }

        public virtual void Attach(SceneView view)
        {
            _sceneView = view;
            _sceneView.RenderSurface.PointerDown += OnMouseDown;
            _sceneView.RenderSurface.PointerUp += OnMouseUp;
            _sceneView.RenderSurface.PointerMove += OnMouseMove;
            _sceneView.RenderSurface.WheelMove += OnWheelMove; ;
        }


        protected Vector3 ToView(PointerEvent ev, float z = -1f)
        {
            var width = _sceneView!.RenderSurface.Size.X;
            var height = _sceneView!.RenderSurface.Size.Y;

            return new Vector3(
                2.0f * ev.X / (float)width - 1.0f,
                1.0f - 2.0f * ev.Y / (float)height,
                z
            );
        }

        protected Vector3 ToWorld(PointerEvent ev, float z = -1f)
        {
            var normPoint = ToView(ev, z);
            return _sceneView!.Camera!.Unproject(normPoint);
        }

        protected Ray3 ToRay(PointerEvent ev)
        {
            var normPoint = ToView(ev);

            var dirEye = Vector4.Transform(new Vector4(normPoint, 1.0f), _sceneView!.Camera!.ProjectionInverse);
            dirEye.W = 0;

            var dirWorld = Vector4.Transform(dirEye, _sceneView!.Camera.WorldMatrix);

            return new Ray3
            {
                Origin = _sceneView!.Camera.WorldPosition,
                Direction = new Vector3(dirWorld.X, dirWorld.Y, dirWorld.Z).Normalize()
            };
        }

        protected virtual void OnWheelMove(PointerEvent ev)
        {

        }
        protected virtual void OnMouseDown(PointerEvent ev)
        {

        }

        protected virtual void OnMouseUp(PointerEvent ev)
        {

        }

        protected virtual void OnMouseMove(PointerEvent ev)
        {

        }

        public virtual void NotifySceneChanged()
        {
            
        }

        public bool IsActive => _isActive;

    }
}
