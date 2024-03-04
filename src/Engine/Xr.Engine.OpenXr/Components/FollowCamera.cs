using System.Numerics;

namespace Xr.Engine.OpenXr
{
    public class FollowCamera : Behavior<Object3D>
    {
        protected override void Update(RenderContext ctx)
        {
            var camera = _host!.Scene!.ActiveCamera!;

            _host.Transform.Position = Offset.Transform(camera.WorldMatrix);
            _host.Transform.Orientation = camera.Transform.Orientation;
                                         

            base.Update(ctx);
        }

        public Vector3 Offset { get; set; }
    }
}
