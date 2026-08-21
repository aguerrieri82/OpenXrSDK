using OpenXr.Framework.Oculus;
using System.Diagnostics;
using System.Numerics;
using XrEngine;
using XrEngine.OpenXr;
using XrMath;

namespace XrSamples.Graffiti
{
    public class InputController : Behavior<MainScene>, IDrawGizmos
    {
        private XrOculusTouchController? _inputs;
        private PaintCanvas? _canvas;
        private PaintSelector? _paintSelector;
        private Can? _can;
        private double _lastScrollTime;
        private double _clearStartTime;

        public void Configure(XrEngineApp e)
        {
            _inputs = e.GetInputs<XrOculusTouchController>();
        }

        public void PlaceSelector(Pose3 gripPose, Vector3 localOffset)
        {
            var forward = (Vector3.UnitY).Transform(gripPose.Orientation);

            forward.Y = 0;
            forward = Vector3.Normalize(forward);

            _paintSelector!.Forward = forward;
            _paintSelector.WorldPosition = gripPose.Position + localOffset;
        }

        protected override void Update(RenderContext ctx)
        {
            Debug.Assert(_inputs?.Right?.Button?.AClick != null);
            Debug.Assert(_inputs?.Right?.Button?.BClick != null);
            Debug.Assert(_inputs?.Right?.ThumbstickClick != null);
            Debug.Assert(_inputs?.Right?.Thumbstick != null);
            Debug.Assert(_inputs?.Right?.GripPose != null);

            _canvas ??= _host!.Scene!.Descendants<PaintCanvas>().First();
            _paintSelector ??= _host!.Scene!.Descendants<PaintSelector>().First();
            _can ??= _host!.Scene!.Descendants<Can>().First();

            var clearButton = _inputs.Right.Button.AClick;
            var selectButton = _inputs.Right.ThumbstickClick;
            var hideButton = _inputs.Right.Button.BClick;

            var clearDt = ctx.Time - _clearStartTime;

            if (clearButton.IsChanged)
            {
                if (clearButton.Value)
                    _clearStartTime = ctx.Time;
                else if (clearDt < 2)
                    _canvas.Undo();
            }
            else if (clearButton.Value && clearDt >= 2 && _clearStartTime > 0)
            {
                _canvas.Clear();
                _clearStartTime = 0;
            }

            if (selectButton.IsChanged && selectButton.Value)
            {
                if (!_paintSelector.IsVisible)
                {
                    var pose = _inputs.Right.GripPose.Value;
                    PlaceSelector(pose, new Vector3(0, 0.03f, 0));
                    _paintSelector.IsVisible = true;
                    _host!.ActiveTool = GraffitiTool.PaintSelector;
                }
                else
                {
                    _can.Color = _paintSelector.Colors[(int)_paintSelector.ActiveIndex];
                    _paintSelector.IsVisible = false;
                    _host!.ActiveTool = GraffitiTool.None;
                }
            }

            if (_paintSelector.IsVisible)
            {
                var scrollValue = _inputs.Right.Thumbstick!.Value!;

                var scrollDir = MathF.Abs(scrollValue.X) > 0.5f ? MathF.Sign(scrollValue.X) : 0;

                var waitTime = 0.3f - (MathF.Abs(scrollValue.X) * 0.2f);

                if (scrollDir != 0 && (ctx.Time - _lastScrollTime) > waitTime)
                {
                    var newIndex = Math.Min(_paintSelector.Colors.Count - 1, Math.Max(0, _paintSelector.ActiveIndex + scrollDir));
                    _paintSelector.SetActiveIndex((int)newIndex);
                    _lastScrollTime = ctx.Time;
                }
            }

            if (hideButton.IsChanged && hideButton.Value)
                _canvas.Frame.IsVisible = !_canvas.Frame.IsVisible;

            base.Update(ctx);
        }

        void IDrawGizmos.DrawGizmos(Canvas3D canvas, RenderContext ctx)
        {
            if (_inputs!.Right!.GripPose!.IsActive)
            {
                var pose = _inputs.Right.GripPose.Value;
                var forward = (-Vector3.UnitY).Transform(pose.Orientation);

                canvas.State.Color = new Color(0, 0, 1);
                canvas.DrawLine(pose.Position, pose.Position + forward);
            }
        }

        bool IDrawGizmos.IsEnabled => false;
    }
}
