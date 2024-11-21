using System.Windows;

namespace XrEditor.Services
{
    public class WpfClipboard : IClipboard
    {
        public void Copy(string text, string mimeType)
        {
            Clipboard.SetText(text);
        }
    }
}
