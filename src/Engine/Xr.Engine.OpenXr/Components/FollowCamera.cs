using OpenXr.Engine;
using OpenXr.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine.OpenXr
{
    public class FollowCamera : Behavior<Object3D>
    {
        protected override void Update(RenderContext ctx)
        {
            var camera = _host!.Scene!.ActiveCamera!;

            var matrix = XrMath.InvertRigidBody(camera.Transform.Matrix);

            _host.Transform.Position = Offset.Transform(matrix);
            _host.Transform.Orientation = Quaternion.Inverse(camera.Transform.Orientation) * 
                                          Quaternion.CreateFromAxisAngle(new Vector3(1f, 0, 0), MathF.PI / 2);

            base.Update(ctx);
        }

        public Vector3 Offset {  get; set; }    
    }
}
