using OpenXr.Framework;

namespace Xr.Editor
{
    public interface IXrGraphicProvider
    {
        IXrGraphicDriver CreateXrDriver();
    }
}
