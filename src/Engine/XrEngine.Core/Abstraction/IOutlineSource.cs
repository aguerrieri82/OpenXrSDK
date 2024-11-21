using XrMath;

namespace XrEngine
{
    public interface IOutlineSource
    {
        bool HasOutlines();

        bool HasOutline(Object3D obj, out Color color);
    }
}
