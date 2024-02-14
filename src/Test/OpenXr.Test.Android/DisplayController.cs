using OpenXr.Engine;
using OpenXr.Framework.Android;
using OpenXr.Framework.Engine;
using OpenXr.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Test.Android
{
    internal class DisplayController : Behavior<Object3D>, IRayTarget, ISurfaceInput
    {
        XrInput<bool> _button;
        double _pointerTime;
        Vector2 _pointer;
        bool _pointerValid;

        public DisplayController(XrInput<bool> button)
        {
            _button = button;
        }

        protected override void Update(RenderContext ctx)
        {
            if (_pointerTime < ctx.Time)
                _pointerValid = false;

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

        public bool IsPointerDown => _button.Value;

    }
}
