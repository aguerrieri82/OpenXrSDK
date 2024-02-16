﻿using OpenXr.Engine;
using OpenXr.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Test.Android
{
    public class DisplayPosition : Behavior<Mesh>  
    {
        bool _isSet;

        protected override void Update(RenderContext ctx)
        {
            if (_isSet)
                return;
            
            var camera = _host!.Scene!.ActiveCamera!;

            var matrix = XrMath.InvertRigidBody(camera.Transform.Matrix);

            _host.Transform.Position = new Vector3(0, 0, -2).Transform(matrix);
            _host.Transform.Orientation = Quaternion.Inverse(camera.Transform.Orientation);

            _isSet = true;

            base.Update(ctx);
        }
    }
}