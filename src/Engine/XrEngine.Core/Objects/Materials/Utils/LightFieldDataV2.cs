using System.Numerics;
using System.Runtime.InteropServices;
using XrMath;

namespace XrEngine
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LightFieldLobe
    {
        public Vector4 Direction;
        public Vector4 Color;
    }

    public class LightFieldDataV2
    {
        public LightFieldDataV2()
        {
            DiffuseStrength = 1;
            SpecularStrength = 1;
        }

        public Texture3D? LookupTexture;

        public LightFieldLobe[]? Contributions;

        public Vector3 Origin;

        public Vector3I Size;

        public float VoxelSize;

        public float DiffuseStrength;

        public float SpecularStrength;

        public long Version;
    }
}