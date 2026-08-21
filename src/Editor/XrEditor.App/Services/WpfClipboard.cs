using System.Windows;
using XrEngine;

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
