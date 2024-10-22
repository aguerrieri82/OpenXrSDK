﻿using XrEditor.Components;
using XrMath;

namespace XrEditor
{
    public class WpfPanelManager : IPanelManager
    {
        public IPopup CreatePopup(ContentView content, Size2I size)
        {
            var result = new WindowPopup();

            result.Content = content;
            result.Width = size.Width;
            result.Height = size.Height;
            return result;
        }
    }
}