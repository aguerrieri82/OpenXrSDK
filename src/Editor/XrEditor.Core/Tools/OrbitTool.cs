using System.Numerics;
using XrEngine;
using XrMath;

namespace XrEditor
{
    public class OrbitToolState : BaseToolState
    {
        public float RotationSpeed;

        public float ZoomSpeed;

        public Vector3 Target;

    }

    public class OrbitTool : BaseMouseTool
    {
        private Spherical _startPos;
        private PointerEvent _startMouse;
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

        protected override void OnMouseDown(PointerEvent ev)
        {
            var camera = (PerspectiveCamera)_sceneView!.Camera!;

            var relPos = (camera.WorldPosition - camera.Target);

            _startMouse = ev;

            if (ev.IsLeftDown)
            {
                _action = OrbitAction.Rotate;
                _startPos = Spherical.FromCartesian(relPos);
                _sceneView.RenderSurface!.CapturePointer();
            }
            else if (ev.IsRightDown)
            {
                _action = OrbitAction.Translate;
                _startWorld = camera.WorldMatrix;
                _startTarget = camera.Target;

                var targetZ = Vector4.Transform(new Vector4(camera.Target, 1), camera.View * camera.Projection);

                _planeZ = targetZ.Z / targetZ.W;
                _startPoint = ToWorld(ev, _planeZ);
                _sceneView.RenderSurface!.CapturePointer();
            }

            base.OnMouseDown(ev);
        }

        protected override void OnWheelMove(PointerEvent ev)
        {
            var camera = (PerspectiveCamera)_sceneView!.Camera!;
            var curDir = (camera.WorldPosition - camera.Target);
            var curLen = curDir.Length();
            var newLen = curLen + curLen * -ev.WheelDelta * 0.001f;

            camera.WorldPosition = camera.Target + curDir.Normalize() * newLen;

        }

        protected override void OnMouseMove(PointerEvent ev)
        {
            var camera = (PerspectiveCamera)_sceneView!.Camera!;

            if (_action == OrbitAction.Rotate)
            {
                var newPos = _startPos;

                newPos.Azm = MathF.Max(0.0001f, MathF.Min(MathF.PI, newPos.Azm - (ev.Y - _startMouse.Y) * RotationSpeed));
                newPos.Pol += (ev.X - _startMouse.X) * RotationSpeed;

                var newPosVec = camera.Target + newPos.ToCartesian();

                camera.LookAt(newPosVec, camera.Target, new Vector3(0, 1, 0));
            }
            else if (_action == OrbitAction.Translate)
            {
                camera.WorldMatrix = _startWorld;

                var newPoint = ToWorld(ev, _planeZ);

                var deltaW = -(newPoint - _startPoint);

                camera.LookAt(_startWorld.Translation + deltaW, _startTarget + deltaW, new Vector3(0, 1, 0));
            }
        }

        protected override void OnMouseUp(PointerEvent ev)
        {
            _sceneView!.RenderSurface!.ReleasePointer();
            _action = OrbitAction.None;
            base.OnMouseUp(ev);
        }

        public float RotationSpeed { get; set; }

        public float ZoomSpeed { get; set; }


    }
}
