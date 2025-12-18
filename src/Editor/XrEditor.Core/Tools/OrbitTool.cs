using System.Numerics;
using XrEngine;
using XrInteraction;
using XrMath;

namespace XrEditor
{

    public class OrbitTool : BasePointerTool
    {
        private Spherical _startPos;
        private Vector2 _startMouse;
        private OrbitAction _action;
        private Matrix4x4 _startWorld;
        private Vector3 _startTarget;
        private Vector3 _startPoint;
        private float _planeZ;

        enum OrbitAction
        {
            None,
            Rotate,
            Translate,
        }

        public OrbitTool()
        {
            RotationSpeed = 0.01f;
            ZoomSpeed = 0.001f;
        }

        protected override void OnPointerDown(Pointer2Event ev)
        {
            if (_sceneView?.Camera == null || _sceneView?.RenderSurface == null)
                return;

            PerspectiveCamera camera = (PerspectiveCamera)_sceneView.Camera;

            Vector3 relPos = (camera.WorldPosition - camera.Target);

            _startMouse = ev.Position;

            if (ev.IsLeftDown)
            {
                _action = OrbitAction.Rotate;
                _startPos = Spherical.FromCartesian(relPos);
                _sceneView.RenderSurface.CapturePointer();
                _sceneView.ActiveTool = this;
            }
            else if (ev.IsRightDown)
            {
                _action = OrbitAction.Translate;
                _startWorld = camera.WorldMatrix;
                _startTarget = camera.Target;

                _planeZ = camera.Project(camera.Target).Z;
                _startPoint = ToWorld(ev, _planeZ);
                _sceneView.RenderSurface.CapturePointer();
                _sceneView.ActiveTool = this;
            }

            base.OnPointerDown(ev);
        }

        protected override void OnWheelMove(Pointer2Event ev)
        {
            if (_sceneView?.Camera == null)
                return;

            PerspectiveCamera camera = (PerspectiveCamera)_sceneView.Camera;
            Vector3 curDir = (camera.WorldPosition - camera.Target);
            float curLen = curDir.Length();
            float newLen = curLen + curLen * -ev.WheelDelta * ZoomSpeed;

            camera.WorldPosition = camera.Target + curDir.Normalize() * newLen;
        }

        protected override void OnPointerMove(Pointer2Event ev)
        {
            if (_sceneView?.Camera == null)
                return;

            if (_sceneView.ActiveTool != this)
                return;

            PerspectiveCamera camera = (PerspectiveCamera)_sceneView.Camera;

            if (_action == OrbitAction.Rotate)
            {
                Spherical newPos = _startPos;

                newPos.Azm = MathF.Max(0.0001f, MathF.Min(MathF.PI, newPos.Azm - (ev.Position.Y - _startMouse.Y) * RotationSpeed));
                newPos.Pol += (ev.Position.X - _startMouse.X) * RotationSpeed;

                Vector3 newPosVec = camera.Target + newPos.ToCartesian();

                camera.LookAt(newPosVec, camera.Target, new Vector3(0, 1, 0));
            }
            else if (_action == OrbitAction.Translate)
            {
                //camera.BeginUpdate();

                camera.WorldMatrix = _startWorld;

                Vector3 newPoint = ToWorld(ev, _planeZ);

                Vector3 deltaW = -(newPoint - _startPoint);

                camera.LookAt(_startWorld.Translation + deltaW, _startTarget + deltaW, new Vector3(0, 1, 0));


                //camera.EndUpdate();
            }
        }

        protected override void OnPointerUp(Pointer2Event ev)
        {
            if (_sceneView?.RenderSurface == null)
                return;

            if (_sceneView.ActiveTool == this)
                _sceneView.ActiveTool = null;

            _sceneView.RenderSurface.ReleasePointer();
            _action = OrbitAction.None;

            base.OnPointerUp(ev);
        }

        public float RotationSpeed { get; set; }

        public float ZoomSpeed { get; set; }


    }
}
