using XrMath;

namespace CanvasUI
{
    public class UiStyle : UiObject
    {

        protected UiElement _owner;

        public UiStyle(UiElement owner)
        {
            _owner = owner;
        }

        protected override void OnPropertyChanged(string propName, object? value, object? oldValue)
        {
            _owner.OnStyleChanged(propName, (IStyleValue)value!, (IStyleValue)oldValue!);
        }



        [UiProperty(UiStyleMode.NotSet)]
        public StyleValue<Color?> BackgroundColor
        {
            get => GetValue<StyleValue<Color?>>(nameof(BackgroundColor));
            set => SetValue(nameof(BackgroundColor), value);
        }

        [UiProperty(UiStyleMode.Inherit)]
        public StyleValue<Color?> Color
        {
            get => GetValue<StyleValue<Color?>>(nameof(Color));
            set => SetValue(nameof(Color), value);
        }

        [UiProperty(UiStyleMode.Inherit, UiPropertyFlags.Layout)]
        public StyleValue<UnitValue> FontSize
        {
            get => GetValue<StyleValue<UnitValue>>(nameof(FontSize));
            set => SetValue(nameof(FontSize), value);
        }

        [UiProperty(UiStyleMode.Inherit, UiPropertyFlags.Layout)]
        public StyleValue<string> FontFamily
        {
            get => GetValue<StyleValue<string>>(nameof(FontFamily));
            set => SetValue(nameof(FontFamily), value);
        }

        [UiProperty(UiStyleMode.Auto, UiPropertyFlags.Layout)]
        public StyleValue<UnitValue> Width
        {
            get => GetValue<StyleValue<UnitValue>>(nameof(Width));
            set => SetValue(nameof(Width), value);
        }

        [UiProperty(UiStyleMode.NotSet, UiPropertyFlags.Layout)]
        public StyleValue<UnitValue> MinWidth
        {
            get => GetValue<StyleValue<UnitValue>>(nameof(MinWidth));
            set => SetValue(nameof(MinWidth), value);
        }

        [UiProperty(UiStyleMode.NotSet, UiPropertyFlags.Layout)]
        public StyleValue<UnitValue> MaxWidth
        {
            get => GetValue<StyleValue<UnitValue>>(nameof(MaxWidth));
            set => SetValue(nameof(MaxWidth), value);
        }

        [UiProperty(UiStyleMode.Auto, UiPropertyFlags.Layout)]
        public StyleValue<UnitValue> Height
        {
            get => GetValue<StyleValue<UnitValue>>(nameof(Height));
            set => SetValue(nameof(Height), value);
        }

        [UiProperty(UiStyleMode.NotSet, UiPropertyFlags.Layout)]
        public StyleValue<UnitValue> MinHeight
        {
            get => GetValue<StyleValue<UnitValue>>(nameof(MinHeight));
            set => SetValue(nameof(MinHeight), value);
        }

        [UiProperty(UiStyleMode.Auto, UiPropertyFlags.Layout)]
        public StyleValue<UnitValue> MaxHeight
        {
            get => GetValue<StyleValue<UnitValue>>(nameof(MaxHeight));
            set => SetValue(nameof(MaxHeight), value);
        }


        [UiProperty(0f, UiPropertyFlags.Layout)]
        public StyleValue<UnitValue> Top
        {
            get => GetValue<StyleValue<UnitValue>>(nameof(Top));
            set => SetValue(nameof(Top), value);
        }

        [UiProperty(0f, UiPropertyFlags.Layout)]
        public StyleValue<UnitValue> Left
        {
            get => GetValue<StyleValue<UnitValue>>(nameof(Left));
            set => SetValue(nameof(Left), value);
        }

        [UiProperty(0f, UiPropertyFlags.Layout)]
        public StyleValue<UnitRectValue> Padding
        {
            get => GetValue<StyleValue<UnitRectValue>>(nameof(Padding));
            set => SetValue(nameof(Padding), value);
        }

        [UiProperty(0f, UiPropertyFlags.Layout)]
        public StyleValue<UnitRectValue> Margin
        {
            get => GetValue<StyleValue<UnitRectValue>>(nameof(Margin));
            set => SetValue(nameof(Margin), value);
        }

        [UiProperty(0f, UiPropertyFlags.Layout)]
        public StyleValue<BorderRectValue> Border
        {
            get => GetValue<StyleValue<BorderRectValue>>(nameof(Border));
            set => SetValue(nameof(Border), value);
        }

        [UiProperty(UiAlignment.Start)]
        public StyleValue<UiAlignment> TextAlign
        {
            get => GetValue<StyleValue<UiAlignment>>(nameof(TextAlign));
            set => SetValue(nameof(TextAlign), value);
        }

        [UiProperty("1em", UiPropertyFlags.Layout)]
        public StyleValue<UnitValue> LineSize
        {
            get => GetValue<StyleValue<UnitValue>>(nameof(LineSize));
            set => SetValue(nameof(LineSize), value);
        }

        [UiProperty(UiTextWrap.Whitespaces, UiPropertyFlags.Layout)]
        public StyleValue<UiTextWrap> TextWrap
        {
            get => GetValue<StyleValue<UiTextWrap>>(nameof(TextWrap));
            set => SetValue(nameof(TextWrap), value);
        }

        [UiProperty(UiVisibility.Visible, UiPropertyFlags.Layout)]
        public StyleValue<UiVisibility> Visibility
        {
            get => GetValue<StyleValue<UiVisibility>>(nameof(Visibility));
            set => SetValue(nameof(Visibility), value);
        }

        [UiProperty(UiOverflow.Hidden, UiPropertyFlags.Layout)]
        public StyleValue<UiOverflow> OverflowX
        {
            get => GetValue<StyleValue<UiOverflow>>(nameof(OverflowX));
            set => SetValue(nameof(OverflowX), value);
        }

        [UiProperty(UiOverflow.Hidden, UiPropertyFlags.Layout)]
        public StyleValue<UiOverflow> OverflowY
        {
            get => GetValue<StyleValue<UiOverflow>>(nameof(OverflowY));
            set => SetValue(nameof(OverflowY), value);
        }

        [UiProperty(UIOrientation.Horizontal, UiPropertyFlags.Layout)]
        public StyleValue<UIOrientation> FlexDirection
        {
            get => GetValue<StyleValue<UIOrientation>>(nameof(FlexDirection));
            set => SetValue(nameof(FlexDirection), value);
        }

        [UiProperty(0f, UiPropertyFlags.Layout)]
        public StyleValue<UnitValue> RowGap
        {
            get => GetValue<StyleValue<UnitValue>>(nameof(RowGap));
            set => SetValue(nameof(RowGap), value);
        }

        [UiProperty(0f, UiPropertyFlags.Layout)]
        public StyleValue<UnitValue> ColGap
        {
            get => GetValue<StyleValue<UnitValue>>(nameof(ColGap));
            set => SetValue(nameof(ColGap), value);
        }

        [UiProperty(UiLayoutType.Flex, UiPropertyFlags.Layout)]
        public StyleValue<UiLayoutType> Layout
        {
            get => GetValue<StyleValue<UiLayoutType>>(nameof(Layout));
            set => SetValue(nameof(Layout), value);
        }

        [UiProperty(UiWrapMode.NoWrap, UiPropertyFlags.Layout)]
        public StyleValue<UiWrapMode> LayoutWrap
        {
            get => GetValue<StyleValue<UiWrapMode>>(nameof(LayoutWrap));
            set => SetValue(nameof(LayoutWrap), value);
        }

        [UiProperty(0f, UiPropertyFlags.Layout)]
        public StyleValue<float> FlexGrow
        {
            get => GetValue<StyleValue<float>>(nameof(FlexGrow));
            set => SetValue(nameof(FlexGrow), value);
        }

        [UiProperty(0f, UiPropertyFlags.Layout)]
        public StyleValue<float> FlexShrink
        {
            get => GetValue<StyleValue<float>>(nameof(FlexShrink));
            set => SetValue(nameof(FlexShrink), value);
        }

        [UiProperty(0f, UiPropertyFlags.Layout)]
        public StyleValue<float> FlexBasis
        {
            get => GetValue<StyleValue<float>>(nameof(FlexBasis));
            set => SetValue(nameof(FlexBasis), value);
        }

        [UiProperty(UiStyleMode.NotSet, UiPropertyFlags.Layout)]
        public StyleValue<UiAlignment?> AlignSelf
        {
            get => GetValue<StyleValue<UiAlignment?>>(nameof(AlignSelf));
            set => SetValue(nameof(AlignSelf), value);
        }

        [UiProperty(UiAlignment.Start, UiPropertyFlags.Layout)]
        public StyleValue<UiAlignment> AlignItems
        {
            get => GetValue<StyleValue<UiAlignment>>(nameof(AlignItems));
            set => SetValue(nameof(AlignItems), value);
        }

        [UiProperty(UiAlignment.Start, UiPropertyFlags.Layout)]
        public StyleValue<UiAlignment> AlignContent
        {
            get => GetValue<StyleValue<UiAlignment>>(nameof(AlignContent));
            set => SetValue(nameof(AlignContent), value);
        }

        [UiProperty(UiAlignment.Start, UiPropertyFlags.Layout)]
        public StyleValue<UiAlignment> JustifyContent
        {
            get => GetValue<StyleValue<UiAlignment>>(nameof(JustifyContent));
            set => SetValue(nameof(JustifyContent), value);
        }

        public Func<UiStyle>? BaseStyle { get; set; }

        public UiElement Owner => _owner;
    }


    public class UIActualStyle : UiStyle
    {
        public UIActualStyle(UiElement owner)
            : base(owner)
        {
        }

        public override T? GetValue<T>(string propName) where T : default
        {
            var parent = _owner.VisualParent;

            if (BaseStyle == null)
            {
                if (parent != null)
                    return parent.ActualStyle.GetValue<T>(propName);

                return default;
            }

            var value = BaseStyle().GetValue<T>(propName);

            if (value is IStyleValue styleValue)
            {
                var curStyle = BaseStyle();

                while (styleValue.Mode == UiStyleMode.NotSet)
                {
                    curStyle = curStyle.BaseStyle?.Invoke();

                    if (curStyle == null)
                        return value;

                    styleValue = (IStyleValue)curStyle.GetValue<T>(propName)!;
                }

                if (styleValue.Mode == UiStyleMode.Inherit || styleValue.Mode == UiStyleMode.NotSet)
                {
                    if (parent != null)
                        return parent.ActualStyle.GetValue<T>(propName);
                }
            }

            return value;
        }

        public override void SetValue<T>(string propName, T? value) where T : default
        {
            throw new NotSupportedException();
        }

    }
}
