using System.Numerics;

namespace XrEngine
{
    public interface IMotionVectorProvider
    {
        Texture2D? Texture { get; }

        Matrix4x4? GetPrevMatrix(Object3D model);

        Matrix4x4[]? GetPrevMatrix(Camera camera);

        void Swap(Camera camera, IEnumerable<Object3D> objects);

        void Begin();

        bool IsActive { get; }
    }
}
