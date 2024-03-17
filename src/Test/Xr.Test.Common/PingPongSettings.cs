using CanvasUI;


namespace Xr.Test
{
    public class PingPongSettings : UIRoot
    {
        public PingPongSettings()
        {
            UiBuilder.From(this).Name("main").AsColumn()
                .Style(s => s.Padding(16).Color("#F5F5F5").BackgroundColor("#050505AF"))
            .BeginRow(s => s.ColGap(16).FlexGrow(1)).Name("cols")
                .BeginColumn(s => s.FlexBasis(1)).Name("col-1")
                    .AddText("Scene 1", s => s.FontSize(1.5f, Unit.Em))
                    .AddCheckBox("Check 1")
                .EndChild()
                .BeginColumn(s => s.FlexBasis(1)).Name("col-2")
                    .AddText("Scene 2")
                .EndChild()
            .EndChild()
            .AddText("Footer");
        }

    }
}
