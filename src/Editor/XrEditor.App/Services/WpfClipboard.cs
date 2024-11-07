using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
