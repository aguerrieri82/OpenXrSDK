using CanvasUI;
using XrEngine;
using XrEngine.OpenXr;

namespace XrSamples
{
    public static class Extension
    {
        public static XrEngineAppBuilder RemovePlaneGrid(this XrEngineAppBuilder builder) => builder.ConfigureApp(e =>
        {
            e.App.ActiveScene!.Descendants<PlaneGrid>().First().IsVisible = false;
        });

        public static IUiBuilder<T> AddInput<T, TValue>(this IUiBuilder<T> builder, string label, IInputElement<TValue> input, IProperty<TValue> binding) where T : UiContainer
        {
            input.Value = binding.Value;
            input.ValueChanged += (_, v, _) => binding.Value = v;

            if (input is CheckBox cb)
            {
                cb.Content = label;
                builder.AddChild(cb);
            }
            else
            {
                builder
                   .BeginColumn()
                       .AddText(label)
                       .AddChild((UiElement)input)
                   .EndChild();
            }

            return builder;
        }
        public static IUiBuilder<T> AddInputRange<T>(this IUiBuilder<T> builder, string label, float min, float max, IProperty<float> binding) where T : UiContainer
        {
            IValueScale scale = (min > 0 && min < 1) ? LogScale.Instance : LinearScale.Instance;

            TextBlock? text = null;

            builder
           .BeginColumn(s=> s.RowGap(4))
               .AddText(label)
               .BeginRow(s=> s.AlignItems(UiAlignment.Center).ColGap(8))

                  .AddText(b => b.Text(binding.Value.ToString())
                                 .Set(e => text = e)
                                 .Style(s=> s
                                        .Width(3, Unit.Em)
                                        .Overflow(UiOverflow.Hidden)
                                        .Padding(4)
                                        .Border(1, "#777")))
                  .AddSlider(b => b.Style(s=> s.FlexGrow(1)).Set(s => 
                  {
                      s.Min = scale.ToScale(min);
                      s.Max = scale.ToScale(max);
                      s.Value = scale.ToScale(binding.Value);
                      s.ValueChanged += (_, v, _) =>
                      {
                          var value = scale.FromScale(v);
                          text!.Text = value.ToString();
                          binding.Value = value;
                      };
                  }))
               .EndChild()
           .EndChild();

            return builder;
        }


    }
}
