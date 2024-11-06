using XrMath;

namespace XrEditor
{
    public enum WindowState
    {

        Normal = 0,

        Minimized = 1,

        Maximized = 2
    }

    public interface IWindow
    {
        void Close();

        Size2 Size { get; set; }

        WindowState State { get; set; }
    }
}
