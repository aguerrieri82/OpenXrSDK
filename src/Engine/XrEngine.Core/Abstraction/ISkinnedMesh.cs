using System.Numerics;
using XrMath;

namespace XrEngine
{
    public interface ISkinnedMesh
    {
        Matrix4x4[] SkinMatrices { get; }

        long SkinMatricesVersion { get; }

        Bounds3 GetWorldBounds();
    }
}
