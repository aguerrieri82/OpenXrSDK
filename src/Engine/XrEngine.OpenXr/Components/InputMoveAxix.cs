using OpenXr.Framework;
using System.Numerics;
using XrMath;

namespace XrEngine.OpenXr
{
    public class InputMoveAxix : Behavior<Object3D>
    {
        protected struct MoveStatus
        {
            public bool IsMoving;

            public Vector3 StartInputPos;

            public Vector3 StartPos;

            public Vector3 LastInputPos;

            public Matrix4x4 WorldInverse;
        }

        MoveStatus _status;

        private Vector3 _startPosition;

        protected override void Start(RenderContext ctx)
        {
            _startPosition = _host!.Transform.Position;
        }

        protected override void Update(RenderContext ctx)
        {
            if (Input == null || Click == null)
            {
                if (XrEngineApp.Current?.Inputs != null)
                    ConfigureInput(XrEngineApp.Current?.Inputs!);
                return;
            }

            if (!Input.IsActive || !Click.IsActive)
                return;

            var curInverse = _status.IsMoving ? _status.WorldInverse : _host!.WorldMatrixInverse;
            var curPos = Input.Value.Position.Transform(curInverse);

            _status.LastInputPos = curPos;

            if (!_status.IsMoving)
            {
                if (!Click.Value)
                    return;

                foreach (var collider in _host!.Components<ICollider3D>().Where(a => a.IsEnabled))
                {
                    if (collider.ContainsPoint(Input.Value.Position, 0.04f))
                    {
                        _status.IsMoving = true;
                        _status.StartInputPos = curPos;
                        _status.StartPos = _host!.Transform.Position;
                        _status.WorldInverse = _host.WorldMatrixInverse;
                        break;
                    }
                }
                return;
            }

            if (!Click.Value)
            {
                _status.IsMoving = false;
                return;
            }

            var handDelta = curPos - _status.StartInputPos;
            var deltaLen = Vector3.Dot(handDelta, Axis);

            var startLen = (_status.StartPos - _startPosition).Length();

            var absLen = startLen + deltaLen;

            absLen = Math.Clamp(absLen, MinDistance, MaxDistance);

            _host!.Transform.Position = _startPosition + Axis * absLen;
        }


        public void ConfigureInput(IXrBasicInteractionProfile input)
        {
            Input = input.Right!.GripPose;
            Click = input.Right!.SqueezeClick;
        }

        public float MinDistance { get; set; }

        public float MaxDistance { get; set; }

        public Vector3 Axis { get; set; }

        public XrPoseInput? Input { get; set; }

        public XrBoolInput? Click { get; set; }

    }
}
