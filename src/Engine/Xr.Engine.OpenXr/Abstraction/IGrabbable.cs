using OpenXr.Engine;
using System.Numerics;

namespace Xr.Engine.OpenXr
{
    public interface IGrabbable : IComponent
    {
        bool CanGrab(Vector3 position);

        void Grab();

        void Release();
    }
}
