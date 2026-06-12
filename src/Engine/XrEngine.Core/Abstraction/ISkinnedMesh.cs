using System.Numerics;

namespace XrEngine
{
    public interface ISkinnedMesh
    {
        SkinData[] Skin { get; }

        Matrix4x4[] SkinMatrices { get; }

        long SkinVersion { get; }

        long SkinMatricesVersion { get; }
    }
}
