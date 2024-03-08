using OpenXr.Framework;

namespace XrEditor
{
    public interface IXrGraphicProvider
    {
        IXrGraphicDriver CreateXrDriver();
    }
}
