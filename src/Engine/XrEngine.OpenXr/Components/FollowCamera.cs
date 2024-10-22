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
                _host!.WorldPosition = Offset.Transform(camera.WorldMatrix);
                _host.WorldOrientation = camera.WorldOrientation;
            }
        }

        public Vector3 Offset { get; set; }
    }
}
