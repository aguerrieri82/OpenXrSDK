using System.Windows;
using XrMath;

namespace XrEditor
{
    public class WpfWindow : IWindow
    {
        private readonly Window _window;

        public WpfWindow(Window window)
        {
            _window = window;
        }

        public Size2 Size
        {
            get => new Size2((float)_window.Width, (float)_window.Height);
            set
            {
                _window.Width = value.Width;
                _window.Height = value.Height;
            }
        }

        public WindowState State
        {
            get => (WindowState)_window.WindowState;
            set => _window.WindowState = (System.Windows.WindowState)value;
        }

        public void Close()
        {
            _window.Close();
        }

    }
}
