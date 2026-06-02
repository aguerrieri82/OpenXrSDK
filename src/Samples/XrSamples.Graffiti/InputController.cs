using OpenXr.Framework.Oculus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using XrEngine;
using XrEngine.OpenXr;
using XrMath;

namespace XrSamples.Graffiti
{
    public class InputController : Behavior<MainScene>, IDrawGizmos
    {
        private  XrOculusTouchController? _inputs;
        private PaintCanvas? _canvas;
        private PaintSelector? _paintSelector;
        private Can? _can;
        private double _lastScrollTime;


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
            Debug.Assert(_inputs?.Right?.ThumbstickClick != null);
            Debug.Assert(_inputs?.Right?.Thumbstick != null);
            Debug.Assert(_inputs?.Right?.GripPose != null);

            _canvas ??= _host!.Scene!.Descendants<PaintCanvas>().First();
            _paintSelector ??= _host!.Scene!.Descendants<PaintSelector>().First();
            _can ??= _host!.Scene!.Descendants<Can>().First();


            var clearButton = _inputs.Right.Button.AClick;
            var thumbClick = _inputs.Right.ThumbstickClick;

            if (clearButton.IsChanged && clearButton.Value)
                _canvas.Clear();

            if (thumbClick.IsChanged && thumbClick.Value)
            {
                if (!_paintSelector.IsVisible)
                {
                    var pose = _inputs.Right.GripPose.Value;
                    PlaceSelector(pose, new Vector3(0, 0.03f, 0));
                    _paintSelector.IsVisible = true;
                }
                else
                {
                    _can.Color = _paintSelector.Colors[(int)_paintSelector.ActiveIndex];
                    _paintSelector.IsVisible = false;
                }
            }

            if (_paintSelector.IsVisible)
            {
                var scrollValue = _inputs.Right.Thumbstick!.Value!;

                var scrollDir = MathF.Abs(scrollValue.X) > 0.5f ? MathF.Sign(scrollValue.X) : 0;
        
                if (scrollDir != 0 && (ctx.Time - _lastScrollTime ) > 0.3f)
                {
                    var newIndex = Math.Min(_paintSelector.Colors.Count - 1, Math.Max(0, _paintSelector.ActiveIndex + scrollDir));
                    _paintSelector.SetActiveIndex((int)newIndex);
                    _lastScrollTime = ctx.Time;
                }
            }

            base.Update(ctx);
        }

        void IDrawGizmos.DrawGizmos(Canvas3D canvas)
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
