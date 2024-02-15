using OpenXr.Engine;
using OpenXr.Framework;
using System.Numerics;
using Xr.Engine.OpenXr;

namespace OpenXr.Test.Android
{
    public class SurfaceController : Behavior<Object3D>, IRayTarget, ISurfaceInput
    {
        readonly XrInput<bool> _mainButton;
        readonly XrInput<bool> _backButton;
        InputButton _mainInBtn;
        InputButton _backInBtn;

        double _pointerTime;
        Vector2 _pointer;
        bool _pointerValid;

        public SurfaceController(XrInput<bool> mainButton, XrInput<bool> backButton)
        {
            _mainButton = mainButton;
            _backButton = backButton;
        }

        protected override void Update(RenderContext ctx)
        {
            if (_pointerTime < ctx.Time)
                _pointerValid = false;

            _mainInBtn.IsDown = _mainButton.Value;
            _mainInBtn.IsChanged = _mainButton.IsChanged;

            _backInBtn.IsDown = _backButton.Value;
            _backInBtn.IsChanged = _backButton.IsChanged;

            base.Update(ctx);
        }

        public void NotifyCollision(RenderContext ctx, Collision collision)
        {
            _pointerTime = ctx.Time;
            _pointer = new Vector2(collision.LocalPoint.X, collision.LocalPoint.Y) + new Vector2(0.5f, 0.5f);
            _pointerValid = true;
        }

        public bool IsPointerValid => _pointerValid;

        public Vector2 Pointer => _pointer;

        public InputButton MainButton => _mainInBtn;

        public InputButton BackButton => _backInBtn;
    }
}
