
using XrMath;

namespace XrEditor
{
    public interface IWindowManager
    {
        IPopup CreatePopup(ContentView content, Size2I size);
    }
}
