
using XrMath;

namespace XrEditor
{
    public interface IPanelManager
    {
        IPopup CreatePopup(ContentView content, Size2I size);
    }
}
