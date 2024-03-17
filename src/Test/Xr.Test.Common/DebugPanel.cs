using CanvasUI;
using XrMath;


namespace Xr.Test
{
    public class DebugPanel : UIRoot
    {
        public DebugPanel()
        {
            UiBuilder.From(this)
            .Style(s => s
                .Padding(16)
                .AlignContent(UiAlignment.Center)
                .AlignItems(UiAlignment.Center)
                .FlexVertical()
                .RowGap(16)
                .BackgroundColor("#FFFFFFA0")
             )

            .AddIcon(IconName.IconJoystick, 46)

            .AddText(t => t
                .Style(s => s
                      .BackgroundColor("#f00")
                      .Border(1, Color.Black)
                      .Height(100, Unit.Perc)
                      .FlexGrow(1)
                      .FlexShrink(1)
                      .Padding(6)
                      .TextAlignCenter()
                      .AlignSelf(UiAlignment.Stretch)
                 )
                .Text("Hello")
                .Set(a => Text1 = a)
            )

            .AddCheckBox(c => c
                .Set(a => a.Content = "Click me")
                .Set(a => Button1 = a)
             );
        }

        public UiCheckBox Button1;

        public UiTextBlock Text1;
    }
}
