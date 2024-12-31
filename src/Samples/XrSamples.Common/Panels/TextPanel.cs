using CanvasUI;

namespace XrSamples
{

    public class TextPanel : UIRoot
    {
        public TextPanel()
        {

            UiBuilder.From(this).Name("main").AsColumn()
            .Style(s => s
                .BackgroundColor("#00000000")
                .Padding(16)
                .Color("#fff")
             )
             .AddText(bld => Text = bld.Element);
        }

        public TextBlock? Text { get; set; }

    }
}
