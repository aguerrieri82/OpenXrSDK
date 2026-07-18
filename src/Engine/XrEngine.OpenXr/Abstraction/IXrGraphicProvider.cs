using OpenXr.Framework;

namespace XrEngine.OpenXr
{
    public interface IXrGraphicProvider
    {
        IXrGraphicDriver CreateXrDriver();
    }
}
