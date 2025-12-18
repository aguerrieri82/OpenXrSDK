using OpenXr.Framework;
using Silk.NET.OpenXR;
using System.Numerics;
using XrMath;

namespace XrEngine.OpenXr
{
    public class InputRotatePivot : Behavior<Object3D>, IDrawGizmos
    {
        protected struct MoveStatus
        {
            public bool IsMoving;
            public Quaternion StartOrientation;
            public Quaternion StartOrientationInput;
            public Vector3 StartDir;
            public Vector3 LastPos;
            public float StartAngle;
            public float StartAngleSign;
        }

        MoveStatus _left;
        MoveStatus _right;


        protected override void Update(RenderContext ctx)
        {
            if (Left == null || Right == null || LeftClick == null || RightClick == null)
                return;

            Process(Left, LeftClick, LeftHaptic, ref _left);
            Process(Right, RightClick, RightHaptic, ref _right);
            Vector3 cart = new Spherical() { Azm = MathF.PI / 2, Pol = MathF.PI / 2, R = 1 }.ToCartesian();

        }


        protected bool Process(XrPoseInput pose, XrBoolInput click, XrHaptic? haptic, ref MoveStatus status)
        {
            if (!pose.IsActive || !click.IsActive || pose.Value.Orientation.W == 0)
                return false;

            Vector3 wordPivot = _host!.ToWorld(LocalPivot);

            Vector3 curDir = (pose.Value.Position - wordPivot).Normalize();

            pose.Value.Orientation.AxisAndAngle(out Vector3 inputDir, out float curAngleUnsigned);

            int curSign = MathF.Sign(Vector3.Dot(curDir, inputDir));

            float curAngle = curAngleUnsigned * (status.IsMoving ? status.StartAngleSign : curSign);

            status.LastPos = pose.Value.Position;

            void Checkpoint(ref MoveStatus status2)
            {
                status2.StartOrientation = _host!.WorldOrientation;
                status2.StartDir = curDir;
                status2.StartOrientationInput = pose.Value.Orientation;
                status2.StartAngle = curAngleUnsigned * curSign;
                status2.StartAngleSign = curSign;
            }

            if (!status.IsMoving)
            {
                if (!click.Value)
                    return false;

                foreach (ICollider3D? collider in _host!.Components<ICollider3D>().Where(a => a.IsEnabled))
                {
                    if (collider.ContainsPoint(pose.Value.Position, 0.04f))
                    {
                        status.IsMoving = true;
                        Checkpoint(ref status);
                        haptic?.VibrateStart(400, 1, TimeSpan.FromMilliseconds(100));
                        break;
                    }
                }

                return false;
            }

            if (!click.Value)
            {
                status.IsMoving = false;
                return false;
            }

            float movAngle = MathF.Acos(status.StartDir.DotNormal(curDir));
            if (movAngle >= MathF.PI / 5 || curSign != status.StartAngleSign)
            {
                Checkpoint(ref status);
                return true;
            }

            float deltaAng = (curAngle - status.StartAngle);

            Quaternion rollRot = MathF.Abs(deltaAng) < 0.0001f ? Quaternion.Identity :
                          Quaternion.CreateFromAxisAngle(curDir, deltaAng);


            _host!.Transform.SetLocalPivot(LocalPivot, true);

            Quaternion newOri = rollRot *
                                status.StartDir.RotationTowards(curDir, Normal) *
                                status.StartOrientation;

            if (ValidateOrientation == null || ValidateOrientation(newOri))
                _host!.WorldOrientation = newOri;

            return true;
        }

        public void DrawGizmos(Canvas3D canvas)
        {
            canvas.Save();

            /*
            var ray = new Ray3() { Origin = Pivot, Direction = Normal }.Transform(_host!.WorldMatrix);

            canvas.State.Color = "#00FF00";
            canvas.DrawLine(ray.PointAt(-0.1f), ray.PointAt(0.5f));
         
            var plane = ray.ToPlane();
            canvas.DrawPlane(plane, ray.Origin, 1, 1, 0.2f);
       
            canvas.State.Color = "#0000FF";
               */
            Vector3 wordPivot = _host!.ToWorld(LocalPivot);


            if (_left.IsMoving)
                canvas.DrawLine(wordPivot, _left.LastPos);

            if (_right.IsMoving)
                canvas.DrawLine(wordPivot, _right.LastPos);

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

        [Action]
        public void StartMove()
        {
            Vector3 wordPivot = _host!.ToWorld(LocalPivot);

            Vector3 curPos = wordPivot + Vector3.UnitX.Transform(_host!.WorldOrientation).Normalize() * 0.3f;

            Vector3 axis = (curPos - wordPivot).Normalize();

            Vector3 direction = axis.Normalize();

            Spherical curDir = Spherical.FromCartesian(direction);

            _left.IsMoving = true;
            _left.StartOrientation = _host!.WorldOrientation;
            _left.LastPos = curPos;
        }


        public Func<Quaternion, bool>? ValidateOrientation { get; set; }

        public Vector3 LocalPivot { get; set; }

        public Vector3 Normal { get; set; }


        public XrPoseInput? Left { get; set; }

        public XrPoseInput? Right { get; set; }

        public XrBoolInput? LeftClick { get; set; }

        public XrBoolInput? RightClick { get; set; }

        public XrHaptic? LeftHaptic { get; set; }

        public XrHaptic? RightHaptic { get; set; }

    }
}
