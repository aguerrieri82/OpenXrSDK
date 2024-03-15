using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine;
using XrEngine.UI;

namespace Xr.Test
{
    public class DebugPanel : UIRoot
    {
        public DebugPanel()
        {
            Style.BackgroundColor = new Color(1, 1, 1, 0.5f);
            Style.Color = Color.Black;
            Style.FontSize = UnitValue.Dp(16);
            Style.Padding = UnitRectValue.All(16);


            Text1 = this.AddChild<UiTextBlock>();
            Text1.SetPosition(10, 10);
            Text1.Text = "Hello";
            Text1.Style.BackgroundColor = new Color(1, 0, 0, 1);
            Text1.Style.Border = BorderRectValue.All(1, Color.Black);
            Text1.Style.Height = UnitValue.Perc(100);
        }

        public UiTextBlock Text1;
    }
}
