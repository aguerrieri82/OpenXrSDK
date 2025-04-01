using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using XrMath;

namespace XrEngine.OpenXr
{
    public class TeleportTarget : BaseComponent<Object3D>, ITeleportTarget
    {
        public bool CanTeleport(Vector3 point)
        {
            var bounds2 = new Bounds2()
            {
                Min = new Vector2(_host!.WorldBounds.Min.X, _host.WorldBounds.Min.Z),
                Max = new Vector2(_host.WorldBounds.Max.X, _host.WorldBounds.Max.Z)
            };
            return point.Y == _host.WorldBounds.Max.Y && bounds2.Contains(new Vector2(point.X, point.Z));
        }

        public IEnumerable<float> GetYPlanes()
        {
            yield return _host!.WorldBounds.Max.Y;
        }
    }
}
