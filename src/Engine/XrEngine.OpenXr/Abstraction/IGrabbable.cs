using System.Numerics;

namespace XrEngine.OpenXr
{
    public interface IGrabbable : IComponent
    {
        bool CanGrab(Vector3 position);

        void Grab();

        void Release();

        void OnMove();
    }
}
