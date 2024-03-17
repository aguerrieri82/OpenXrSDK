namespace CanvasUI
{
    public static class UiBuilderExtensions
    {

        public static IUiBuilder<UiTextBlock> Text(this IUiBuilder<UiTextBlock> builder, string value)
        {
            builder.Element.Text = value;
            return builder;
        }

        public static IUiBuilder<T> Content<T>(this IUiBuilder<T> builder, object? value) where T : UiContentView
        {
            builder.Element.Content = value;
            return builder;
        }

        public static IUiBuilder<T> AddButton<T>(this IUiBuilder<T> builder, Action<IUiBuilder<UiButton>>? build = null) where T : UiContainer
        {
            return builder.Child(build);
        }

        public static IUiBuilder<T> AddText<T>(this IUiBuilder<T> builder, Action<IUiBuilder<UiTextBlock>>? build = null) where T : UiContainer
        {
            return builder.Child(build);
        }

        public static IUiBuilder<T> AddText<T>(this IUiBuilder<T> builder, string text, Action<UiStyleBuilder>? style = null) where T : UiContainer
        {
            return builder.Child<T, UiTextBlock>(b => b.Text(text).Style(style));
        }

        public static IUiBuilder<T> AddCheckBox<T>(this IUiBuilder<T> builder, Action<IUiBuilder<UiCheckBox>>? build = null) where T : UiContainer
        {
            return builder.Child(build);
        }

        public static IUiBuilder<T> AddCheckBox<T>(this IUiBuilder<T> builder, object? content) where T : UiContainer
        {
            return builder.Child<T, UiCheckBox>(b => b.Element.Content = content);
        }

        public static IUiBuilder<T> AddIcon<T>(this IUiBuilder<T> builder, Action<IUiBuilder<UiIcon>>? build = null) where T : UiContainer
        {
            return builder.Child(build);
        }

        public static IUiBuilder<T> AddIcon<T>(this IUiBuilder<T> builder, IconName icon, float size = 24) where T : UiContainer
        {
            return builder.Child<T, UiIcon>(b =>
            {
                b.Element.Icon = icon;
                b.Element.Style.FontSize = UnitValue.Get(size);
            });
        }

        public static IUiBuilder<T> AddSlider<T>(this IUiBuilder<T> builder, Action<IUiBuilder<UiSlider>>? build = null) where T : UiContainer
        {
            return builder.Child(build);
        }

        public static IUiBuilder<T> AddSlider<T>(this IUiBuilder<T> builder, float min = 0, float max = 1, float step = 0) where T : UiContainer
        {
            return builder.Child<T, UiSlider>(b =>
            {
                b.Element.Min = min;
                b.Element.Max = max;
                b.Element.Step = step;
            });
        }

    }
}
