namespace CanvasUI
{
    public enum UiStyleSelectorType
    {
        Class,
        Name,
        Type,
        Any,
    }

    public struct UiSelectorValue
    {
        public UiStyleSelectorType Type;

        public string Value;

        public bool IsDirectChild;
    }

    public class UiStyleSelector
    {
        public List<UiSelectorValue> Values { get; } = [];
    }


    public class UiStyleRule
    {
        public List<UiStyleSelector>? Selectors { get; } = [];

        public UiStyle? Style { get; set; }
    }
}
