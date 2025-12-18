using XrEditor.Components;
using XrMath;

namespace XrEditor
{
    public class WpfWindowManager : IWindowManager
    {
        public IPopup CreatePopup(ContentView content, Size2I size)
        {
            var result = new WindowPopup
            {
                Content = content,
                Width = size.Width,
                Height = size.Height
            };
            return result;
        }
    }
}
