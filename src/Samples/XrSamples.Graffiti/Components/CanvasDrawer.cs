using OpenXr.Framework.Oculus;
using System.Diagnostics;
using System.Numerics;
using XrEngine;
using XrEngine.OpenXr;

namespace XrSamples.Graffiti
{
    public class CanvasDrawer : Behavior<MainScene>
    {
        enum State
        {
            Point1,
            Point2,
            Finish
        }

        State _state;
        Vector3 _point1;
        Vector3 _point2;
        private XrOculusTouchController? _inputs;
        private PaintFrame? _frame;
        private PaintCanvas? _canvas;

        public void Configure(XrEngineApp e)
        {
            _inputs = e.GetInputs<XrOculusTouchController>();
        }

        protected override void OnEnabled()
        {
            _state = State.Point1;
            base.OnEnabled();
        }

        protected void Draw()
        {
            var worldUp = Vector3.UnitY;

            var center = (_point1 + _point2) * 0.5f;
            var diagonal = _point2 - _point1;

            var vertical = Vector3.Dot(diagonal, worldUp);
            var horizontal = diagonal - worldUp * vertical;

            var width = horizontal.Length();
            var height = MathF.Abs(vertical);

            if (width < 1e-6f || height < 1e-6f)
                return;

            var right = Vector3.Normalize(horizontal);

            // If the user touched bottom-right -> top-left instead of top-left -> bottom-right,
            // horizontal points the opposite way. That is still a valid 180° rotated frame.
            var up = worldUp;

            var forward = Vector3.Normalize(Vector3.Cross(right, up));

            var transform = new Matrix4x4(
                right.X, right.Y, right.Z, 0,
                up.X, up.Y, up.Z, 0,
                forward.X, forward.Y, forward.Z, 0,
                center.X, center.Y, center.Z, 1
            );

            if (_frame == null)
            {
                _frame = new PaintFrame(new Vector2(width, height), new PbrMaterial { Color = new XrMath.Color(1, 0, 0) });
                _frame.Name = "DrawFrame";
                _host!.Scene!.AddChild(_frame);
            }
            else
            {
                _frame.Size = new Vector2(width, height);
                _frame.Build();
            }

            _frame.IsVisible = true;
            _frame.WorldMatrix = transform;
        }

        protected override void Update(RenderContext ctx)
        {
            Debug.Assert(_inputs?.Right?.Thumbstick != null);
            Debug.Assert(_inputs?.Right?.GripPose != null);

            var controller = _inputs.Right.GripPose;
            var thumb = _inputs.Right.Thumbstick;

            if (!controller.IsActive)
                return;

            _canvas ??= _host!.Descendants<PaintCanvas>().First();

            var isChanged = _inputs!.Right!.SqueezeClick!.IsChanged;
            var isOn = _inputs!.Right!.SqueezeClick.Value;

            if (_state == State.Point1 && isChanged)
            {
                if (isOn)
                {
                    _point1 = controller.Value.Position;
                    _point2 = controller.Value.Position;
                    _state = State.Point2;

                    _host!.ActiveTool = GraffitiTool.CanvasDraw;
                }
            }

            else if (_state == State.Point2)
            {
                _point2 = controller.Value.Position;

                Draw();

                if (!isOn && isChanged)
                {
                    _state = State.Point1;
                    _frame?.IsVisible = false;

                    _canvas.SetCanvasSize(_frame!.Size, _frame.GetWorldPose(), _canvas.TexelSize);

                    _host!.ActiveTool = GraffitiTool.None;
                }
            }

            if (thumb.IsChanged && _host!.ActiveTool == GraffitiTool.None)
            {
                var value = thumb.Value;

                if (MathF.Abs(value.Y) > 0.2f)
                {
                    var forward = _canvas.Forward;
                    var newPos = _canvas.WorldPosition + (forward * value.Y * 0.002f);
                    _canvas.WorldPosition = newPos;
                }
            }

        }
    }
}
