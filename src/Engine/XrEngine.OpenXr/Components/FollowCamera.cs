using System.Numerics;
using XrMath;

namespace XrEngine.OpenXr
{
    public class FollowCamera : Behavior<Object3D>
    {
        protected override void Update(RenderContext ctx)
        {
            var camera = _host?.Scene?.ActiveCamera;

            if (camera != null)
            {
                _host!.Transform.Position = Offset.Transform(camera.WorldMatrix);
                _host.Transform.Orientation = camera.Transform.Orientation;
            }

        }

        public Vector3 Offset { get; set; }
    }
}
