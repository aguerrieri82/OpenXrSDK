using System.Numerics;

namespace XrEngine
{
    public interface IQuodTexture
    {
        Matrix4x4 WorldMatrix { get; }

        Texture2D? DrawTexture { get; }

        Texture2D? ActiveTexture { get; }

        int ActiveEye { get; }

        float DepthBias { get; }

        bool EnableDepthCull { get; }
    }
}
