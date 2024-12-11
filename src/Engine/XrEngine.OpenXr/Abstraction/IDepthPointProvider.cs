using System.Numerics;

namespace XrEngine.OpenXr
{
    public interface IDepthPointProvider
    {
        Vector3[]? ReadPoints(IEnvDepthProvider provider);
    }
}
