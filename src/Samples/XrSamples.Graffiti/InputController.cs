using OpenXr.Framework.Oculus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using XrEngine;
using XrEngine.OpenXr;

namespace XrSamples.Graffiti
{
    public class InputController : Behavior<MainScene>
    {
        private  XrOculusTouchController? _inputs;
        private PaintCanvas? _canvas;

        public void Configure(XrEngineApp e)
        {
            _inputs = e.GetInputs<XrOculusTouchController>();
        }

        protected override void Update(RenderContext ctx)
        {
            Debug.Assert(_inputs?.Right?.Button?.AClick != null);

            _canvas ??= _host!.Scene!.Descendants<PaintCanvas>().First();

            var clearButton = _inputs.Right.Button.AClick;

            if (clearButton.IsChanged && clearButton.Value)
                _canvas.Clear();

            base.Update(ctx);
        }
    }
}
