using System.Numerics;

namespace XrEngine
{
    public interface ISkinnedMesh
    {
        Matrix4x4[] SkinMatrices { get; }

        long SkinMatricesVersion { get; }
    }
}
