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
            Style.Padding = UnitRectValue.All(16);
            Style.ColGap = UnitValue.Dp(16);
            Style.AlignContent = UiAlignment.Start;
            Style.AlignItems = UiAlignment.Center;

            var icon = this.AddChild<UiIcon>();
            icon.Icon = IconName.IconJoystick;
            icon.Style.FontSize = UnitValue.Dp(46);

            Text1 = this.AddChild<UiTextBlock>();
            Text1.SetPosition(10, 10);
            Text1.Text = "Hello";
            //Text1.Style.Margin = new UnitRectValue() { Top = 30f };
            Text1.Style.BackgroundColor = new Color(1, 0, 0, 1);
            Text1.Style.Border = BorderRectValue.All(1, Color.Black);
            Text1.Style.Height = UnitValue.Perc(100);
            Text1.Style.FlexGrow = UnitValue.Dp(1);
            Text1.Style.FlexShrink = UnitValue.Dp(1);
            Text1.Style.Padding = UnitRectValue.All(16);
            Text1.Style.TextAlign = UiAlignment.Center;

            Button1 = this.AddChild<UiButton>();
            Button1.Content = "Click Mep";
            Button1.Style.BackgroundColor = new Color(0, 1, 0, 1);
        }

        public UiButton Button1;

        public UiTextBlock Text1;
    }
}
