using OpenXr.Framework;
using System.Numerics;
using XrEngine;
using XrMath;

namespace XrEngine.OpenXr
{
    public class InputRotateAxis : Behavior<Object3D>, IDrawGizmos
    {
        protected struct MoveStatus
        {
            public bool IsMoving;
            public float StartAngle;
            public Vector3 StartPos;
            public Vector3 StartDir;
            public Vector3 LastPos;
        }

        MoveStatus _left;
        MoveStatus _right;
        Quaternion _startOrientation;

        protected float _angle;

        protected override void Start(RenderContext ctx)
        {
            _startOrientation = _host!.Transform.Orientation;
        }

        protected override void Update(RenderContext ctx)
        {
            if (Left == null || Right == null || LeftClick == null || RightClick == null)
                return;

            var a1 = Process(Left, LeftClick, LeftHaptic, ref _left);
            var a2 = Process(Right, RightClick, RightHaptic, ref _right);
            
            var curRot = MathF.Min(a1 ?? float.PositiveInfinity, a2 ?? float.PositiveInfinity); 


            if (float.IsFinite(curRot))
            {
                Log.Value("Rotation", curRot);
                ApplyRotation(curRot);
            }
        }

        protected void ApplyRotation(float angle)
        {
            angle = MathF.Min(MaxAngle, MathF.Max(MinAngle, angle));

            _host!.Transform.SetLocalPivot(RotationAxis.Origin, true);
            _host!.Transform.Orientation = _startOrientation * Quaternion.CreateFromAxisAngle(RotationAxis.Direction, angle);
            
            _angle = angle;
            _left.StartAngle = angle;
            _right.StartAngle = angle;
        }

        protected float? Process(XrPoseInput pose, XrBoolInput click, XrHaptic? haptic, ref MoveStatus status)
        {
            if (!pose.IsActive || !click.IsActive)
                return null;

            var plane = RotationAxis.ToPlane();
            var curPos = _host!.ToLocal(pose.Value.Position);
            var planePos = plane.Project(curPos);
            var curDir = (planePos - RotationAxis.Origin).Normalize();

            status.LastPos = curPos;

            if (!status.IsMoving)
            {
                if (!click.Value)
                    return null;

                foreach (var collider in _host!.Components<ICollider3D>().Where(a=> a.IsEnabled))
                {
                    if (collider.ContainsPoint(pose.Value.Position, 0.04f))
                    {
                        status.IsMoving = true; 
                        status.StartAngle = _angle;
                        status.StartPos = curPos;
                        status.StartDir = curDir;

                        haptic?.VibrateStart(400, 1, TimeSpan.FromMilliseconds(100));

                        break;
                    }
                }

                return null;
            }

            if (!click.Value)
            {
                status.IsMoving = false;
                return null;
            }

            var distance = Vector3.Distance(_host!.ToWorld(status.StartPos), pose.Value.Position);
            if (distance > MaxDistance)
                return null;

            return status.StartAngle - curDir.SignedAngleWith(status.StartDir, plane.Normal);
        }

        public void DrawGizmos(Canvas3D canvas)
        {
            canvas.Save();
            
            canvas.State.Transform = _host!.WorldMatrix;

            canvas.State.Color = "#00FF00";
            canvas.DrawLine(RotationAxis.PointAt(-0.5f), RotationAxis.PointAt(0.5f));
            /*
            var plane = RotationAxis.ToPlane();
            canvas.DrawPlane(plane, RotationAxis.Origin, 1, 1, 0.2f);
            */
            canvas.State.Color = "#0000FF";

            if (_left.IsMoving)
                canvas.DrawLine(_left.StartPos, _left.LastPos);
            if (_right.IsMoving)
                canvas.DrawLine(_right.StartPos, _right.LastPos);

            canvas.Restore();
        }
        public void ConfigureInput(IXrBasicInteractionProfile input)
        {
            Left = input.Left!.GripPose;
            Right = input.Right!.GripPose;
            LeftClick = input.Left!.SqueezeClick;
            RightClick = input.Right!.SqueezeClick;
            LeftHaptic = input.Left!.Haptic;
            RightHaptic = input.Right!.Haptic;
        }

        public float Angle
        {
            get => _angle;
            set
            {
                if (_angle == value)
                    return;
                _angle = value;
                ApplyRotation(value);   
            }
        }

        public Ray3 RotationAxis { get; set; }

        [ValueType(XrEngine.ValueType.Radiant)]
        public float MinAngle { get; set; }

        [ValueType(XrEngine.ValueType.Radiant)]
        public float MaxAngle { get; set; }

        public float MaxDistance { get; set; }

        public XrPoseInput? Left { get; set; }

        public XrPoseInput? Right { get; set; }

        public XrBoolInput? LeftClick { get; set; }

        public XrBoolInput? RightClick { get; set; }

        public XrHaptic? LeftHaptic { get; set; }

        public XrHaptic? RightHaptic { get; set; }

    }
}
