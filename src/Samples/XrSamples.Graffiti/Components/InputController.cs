using OpenXr.Framework;
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
        private ToolSelector? _paintSelector;
        private Can? _can;
        private SprayTracker? _tracker;
        private UIWebPanel? _webPanel;
        private RayPointerCollider? _rayCollider;
        private XrRoot? _xrRoot;
        private int _lastThumbStep;
        private double _lastScrollTime;
        private double _clearStartTime;
        private float _lastTrigger;
        private bool _hasTestStroke;
        private bool _isFrameTogglePending;

        public void Configure(XrEngineApp e)
        {
            _inputs = e.GetInputs<XrOculusTouchController>();
        }

        public void PlaceSelector()
        {
            var camera = _host.ActiveCamera!;
            var forward = camera.Forward;

            forward.Y = 0;
            forward = Vector3.Normalize(forward);

            _paintSelector!.Forward = -forward;
            _paintSelector.WorldPosition = camera.WorldPosition + forward * 0.15f - new Vector3(0, 0.2f, 0);
        }

        protected override void Update(RenderContext ctx)
        {
            

            Debug.Assert(_inputs?.Right?.Button?.AClick != null);
            Debug.Assert(_inputs?.Right?.Button?.BClick != null);
            Debug.Assert(_inputs?.Right?.ThumbstickClick != null);
            Debug.Assert(_inputs?.Right?.Thumbstick != null);
            Debug.Assert(_inputs?.Right?.GripPose != null);

            _canvas ??= _host.Scene!.Descendants<PaintCanvas>().First();
            _paintSelector ??= _host.Scene!.Descendants<ToolSelector>().First();
            _can ??= _host.Scene!.Descendants<Can>().First();

            _tracker ??= _can.Component<SprayTracker>();
            _webPanel ??= _host.Scene!.Descendants<UIWebPanel>().First();
            _rayCollider ??= _host.Scene!.Component<RayPointerCollider>();
            _xrRoot ??= _host.Scene!.Descendants<XrRoot>().First();

            var clearButton = _inputs.Right.Button.BClick;
            var selectButton = _inputs.Right.ThumbstickClick;
            var hideButton = _inputs.Right.Button.AClick;
            var trigger = _inputs.Right.TriggerValue?.Value ?? 0;

            if (selectButton.IsChanged && selectButton.Value && _host.ActiveTool != GraffitiTool.CanvasPlace)
            {
                if (!_paintSelector.IsVisible)
                {
                    PlaceSelector();
                    _paintSelector.IsVisible = true;
                    _host.ActiveTool = GraffitiTool.PaintSelector;
                }
                else
                    CloseSelector();
            }

            if (_paintSelector.IsVisible)
            {
                var thumbValue = _inputs.Right.Thumbstick!.Value!;

                var thumbStep = MathF.Abs(thumbValue.Y) > 0.85f ? MathF.Sign(thumbValue.Y) : 0;

                if (thumbStep != 0 && thumbStep != _lastThumbStep)
                    _paintSelector.Mode = (ToolSelectorMode)Math.Clamp((int)_paintSelector.Mode + thumbStep, 0, (int)ToolSelectorMode.WebUI);

                _lastThumbStep = thumbStep;
            }

            if (_paintSelector.IsVisible && _paintSelector.Mode != ToolSelectorMode.WebUI)
            {
                var thumbValue = _inputs.Right.Thumbstick!.Value!;

                var scrollDir = MathF.Abs(thumbValue.X) > 0.5f ? MathF.Sign(thumbValue.X) : 0;

                var waitTime = 0.3f - (MathF.Abs(thumbValue.X) * 0.2f);

                if (scrollDir != 0 && trigger == 0 && (ctx.Time - _lastScrollTime) > waitTime)
                {
                    var ring = _paintSelector.ActiveRing;
                    var oldIndex = ring.ActiveIndex;

                    ring.SetActiveIndex(oldIndex + scrollDir);

                    if (ring.ActiveIndex != oldIndex)
                    {
                        DiscardTestStroke();
                        _can.Color = _paintSelector.ColorRing.ActiveColor;
                        _can.Cap = _paintSelector.CapRing.ActiveCap;
                    }

                    _lastScrollTime = ctx.Time;
                }

                if (clearButton.IsChanged && clearButton.Value)
                    _paintSelector.Preview.Distance += 0.05f;

                if (hideButton.IsChanged && hideButton.Value)
                    _paintSelector.Preview.Distance -= 0.05f;

                if (trigger > 0 && _lastTrigger == 0)
                {
                    DiscardTestStroke();
                    _hasTestStroke = true;
                }
            }
            else if (!_paintSelector.IsVisible && _host.ActiveTool != GraffitiTool.CanvasPlace)
            {
                var clearDt = ctx.Time - _clearStartTime;

                if (clearButton.IsChanged)
                {
                    if (clearButton.Value)
                        _clearStartTime = ctx.Time;
                    else if (clearDt < 2)
                        _ = _canvas.UndoAsync();
                }
                else if (clearButton.Value && clearDt >= 2 && _clearStartTime > 0)
                {
                    _ = _canvas.ClearAsync();
                    _clearStartTime = 0;
                }

                if (hideButton.IsChanged)
                {
                    if (hideButton.Value)
                        _isFrameTogglePending = true;
                    else if (_isFrameTogglePending)
                    {
                        _canvas.Frame.IsVisible = !_canvas.Frame.IsVisible;
                        _isFrameTogglePending = false;
                    }
                }
            }

            if (_host.ActiveTool == GraffitiTool.CanvasDraw)
                _isFrameTogglePending = false;

            _tracker.IsEnabled = _paintSelector.IsVisible && _paintSelector.Mode == ToolSelectorMode.Cap;

            var isWebMode = _paintSelector.IsVisible && _paintSelector.Mode == ToolSelectorMode.WebUI;

            if (isWebMode && !_webPanel.IsVisible)
            {
                DiscardTestStroke();

                var forward = _paintSelector.WorldPosition - _host.ActiveCamera!.WorldPosition;
                forward.Y = 0;
                forward = Vector3.Normalize(forward);

                _webPanel.WorldPosition = _paintSelector.WorldPosition + new Vector3(0, 0.2f, 0) + forward * 0.2f;
                _webPanel.WorldOrientation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.Atan2(-forward.X, -forward.Z));
            }

            _webPanel.IsVisible = isWebMode;

            var isPointerMode = isWebMode || _host.ActiveTool == GraffitiTool.CanvasPlace;

            var isHands = XrApp.Current!.RightIntProfile?.Contains("hand") == true;

            _can.IsVisible = !isPointerMode && !isHands;
            _rayCollider.IsEnabled = isPointerMode;
            _rayCollider.RayView.IsVisible = isPointerMode;
            _xrRoot.RightControllerMode = isPointerMode ? XrControllerMode.Auto : XrControllerMode.Hidden;

            _lastTrigger = trigger;

            base.Update(ctx);
        }

        public void CloseSelector()
        {
            DiscardTestStroke();
            _paintSelector!.IsVisible = false;
            _host.ActiveTool = GraffitiTool.None;
        }

        void DiscardTestStroke()
        {
            if (!_hasTestStroke)
                return;

            _ = _canvas!.DiscardTestStrokeAsync();
            _hasTestStroke = false;
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
