using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using XrEngine;

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

        protected override void OnEnabled()
        {
            _state = State.Point1;
            base.OnEnabled();
        }


        protected override void Update(RenderContext ctx)
        {
            base.Update(ctx);
        }
    }
}
