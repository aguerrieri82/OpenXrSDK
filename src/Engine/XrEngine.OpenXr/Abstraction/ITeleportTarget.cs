using System.Numerics;

namespace XrEngine.OpenXr
{
    public interface ITeleportTarget
    {
        bool CanTeleport(Vector3 point);

        IEnumerable<float> GetYPlanes();
    }
}
