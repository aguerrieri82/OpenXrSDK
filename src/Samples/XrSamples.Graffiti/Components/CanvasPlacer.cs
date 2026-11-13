using OpenXr.Framework.Oculus;
using System.Diagnostics;
using System.Numerics;
using XrEngine;
using XrEngine.OpenXr;
using XrMath;

namespace XrSamples.Graffiti
{
    public enum CanvasPlacerMode
    {
        Place,
        Draw
    }

    public class CanvasPlacer : Behavior<MainScene>
    {
        private XrOculusTouchController? _inputs;
        private PaintCanvas? _canvas;
        private IEnvRayCollider? _rayCollider;
        private HitCross? _cross;
        private PaintFrame? _frame;
        private bool _isPlaced;
        private bool _isDrawing;
        private Vector3 _point1;
        private Vector3 _point2;
        private Vector3 _normal;

        public CanvasPlacer()
        {
            IsEnabled = false;
        }

        public void Configure(XrEngineApp e)
        {
            _inputs = e.GetInputs<XrOculusTouchController>();
        }

        protected override void OnEnabled()
        {
            _host.ActiveTool = GraffitiTool.CanvasPlace;
            _isPlaced = false;
            _isDrawing = false;

            _rayCollider ??= Context.Require<IEnvRayCollider>();
            _rayCollider.IsEnabled = true;

            _canvas ??= _host.Descendants<PaintCanvas>().First();
            _canvas.IsVisible = Mode == CanvasPlacerMode.Place;

            base.OnEnabled();
        }

        protected override void OnDisabled()
        {
            _rayCollider?.IsEnabled = false;
            _canvas?.IsVisible = true;
            _cross?.IsVisible = false;
            _frame?.IsVisible = false;
            base.OnDisabled();
        }

        protected override void Update(RenderContext ctx)
        {
            Debug.Assert(_inputs?.Right?.AimPose != null);
            Debug.Assert(_inputs?.Right?.TriggerClick != null);

            Debug.Assert(_canvas != null);

            _cross ??= _host.AddChild(new HitCross());

            if (_isPlaced)
            {
                if ((_inputs.Right.TriggerValue?.Value ?? 0) == 0)
                {
                    _host.ActiveTool = GraffitiTool.None;
                    IsEnabled = false;
                }

                return;
            }

            var aim = _inputs.Right.AimPose;

            if (!aim.IsActive)
                return;

            var ray = new Ray3
            {
                Origin = aim.Value.Position,
                Direction = (-Vector3.UnitZ).Transform(aim.Value.Orientation)
            };

            var isHit = _rayCollider!.CastRay(ray, 10, out var hit);

            var normal = Vector3.UnitZ.Transform(hit.Orientation);
            normal.Y = 0;

            isHit = isHit && normal.LengthSquared() > 0.0001f;

            if (isHit)
            {
                normal = Vector3.Normalize(Vector3.Dot(normal, ray.Direction) > 0 ? -normal : normal);

                if (!_isDrawing)
                    _normal = normal;
                else
                    _point2 = hit.Position;
            }

            var diagonal = _point2 - _point1;
            var flat = new Vector3(diagonal.X, 0, diagonal.Z);
            var planeNormal = _normal;

            if (_isDrawing && flat.LengthSquared() > 0.0001f)
            {
                planeNormal = Vector3.Normalize(Vector3.Cross(flat, Vector3.UnitY));

                if (Vector3.Dot(planeNormal, _normal) < 0)
                    planeNormal = -planeNormal;
            }

            var right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, planeNormal));

            var orientation = Quaternion.CreateFromRotationMatrix(new Matrix4x4(
                right.X, right.Y, right.Z, 0,
                0, 1, 0, 0,
                planeNormal.X, planeNormal.Y, planeNormal.Z, 0,
                0, 0, 0, 1));

            var trigger = _inputs.Right.TriggerClick;

            if (Mode == CanvasPlacerMode.Place)
            {
                if (isHit)
                {
                    _canvas.WorldPosition = hit.Position;
                    _canvas.WorldOrientation = orientation;
                }

                if (trigger.IsChanged && trigger.Value)
                    AnchorCanvas();

                return;
            }

            if (!_isDrawing)
            {
                _cross.IsVisible = isHit;

                if (isHit)
                {
                    _cross.WorldPosition = hit.Position + _normal * 0.002f;
                    _cross.WorldOrientation = orientation;
                }

                if (isHit && trigger.IsChanged && trigger.Value)
                {
                    _point1 = hit.Position;
                    _point2 = hit.Position;
                    _isDrawing = true;
                }

                return;
            }

            var horizontal = Vector3.Dot(diagonal, right);

            var size = new Vector2(MathF.Abs(horizontal), MathF.Abs(diagonal.Y));

            var pose = new Pose3
            {
                Position = _point1 + right * (horizontal * 0.5f) + Vector3.UnitY * (diagonal.Y * 0.5f),
                Orientation = orientation
            };

            if (_frame == null)
            {
                _frame = _host.AddChild(new PaintFrame(size, new PbrMaterial { Color = new Color(1, 0, 0) }));
                _frame.Name = "WallFrame";
            }
            else
            {
                _frame.Size = size;
                _frame.Build();
            }

            _frame.IsVisible = true;
            _frame.SetWorldPose(pose);

            if (trigger.IsChanged && !trigger.Value)
            {
                _frame.IsVisible = false;
                _cross.IsVisible = false;

                if (size.X < 0.05f || size.Y < 0.05f)
                {
                    _isDrawing = false;
                    return;
                }

                _canvas.SetCanvasSize(size, pose, _canvas.TexelSize);

                AnchorCanvas();
            }
        }

        void AnchorCanvas()
        {
            Debug.Assert(_canvas != null);

            var pose = _canvas.GetWorldPose();
            var grid = _canvas.Component<SpatialAnchorGrid>();
            var anchor = grid.GetClosestAnchor(pose.Position, out _);

            if (anchor != null)
                grid.SetReferenceAnchor(anchor.Id, anchor.CurrentWorldPose.Inverse().Multiply(pose));

            _isPlaced = true;
        }

        public CanvasPlacerMode Mode { get; set; }
    }
}
