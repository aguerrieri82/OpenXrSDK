﻿using System.Numerics;
using XrEngine;
using XrMath;

namespace XrSamples.Android
{
    public class DisplayPosition : Behavior<TriangleMesh>
    {
        bool _isSet;

        protected override void Update(RenderContext ctx)
        {
            if (_isSet)
                return;

            var camera = _host!.Scene!.ActiveCamera!;

            _host.Transform.Position = new Vector3(0, 0, -2).Transform(camera.Transform.Matrix);
            _host.Transform.Orientation = camera.Transform.Orientation;

            _isSet = true;
        }
    }
}
