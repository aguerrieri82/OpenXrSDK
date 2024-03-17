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
            _owner.OnStyleChanged(propName, (IUiStyleValue)value!, (IUiStyleValue)oldValue!);
        }



        [UiProperty(UiStyleMode.NotSet)]
        public UiStyleValue<Color?> BackgroundColor
        {
            get => GetValue<UiStyleValue<Color?>>(nameof(BackgroundColor));
            set => SetValue(nameof(BackgroundColor), value);
        }

        [UiProperty(UiStyleMode.Inherit)]
        public UiStyleValue<Color?> Color
        {
            get => GetValue<UiStyleValue<Color?>>(nameof(Color));
            set => SetValue(nameof(Color), value);
        }

        [UiProperty(UiStyleMode.Inherit, UiPropertyFlags.Layout)]
        public UiStyleValue<UnitValue> FontSize
        {
            get => GetValue<UiStyleValue<UnitValue>>(nameof(FontSize));
            set => SetValue(nameof(FontSize), value);
        }

        [UiProperty(UiStyleMode.Inherit, UiPropertyFlags.Layout)]
        public UiStyleValue<string> FontFamily
        {
            get => GetValue<UiStyleValue<string>>(nameof(FontFamily));
            set => SetValue(nameof(FontFamily), value);
        }

        [UiProperty(UiStyleMode.Auto, UiPropertyFlags.Layout)]
        public UiStyleValue<UnitValue> Width
        {
            get => GetValue<UiStyleValue<UnitValue>>(nameof(Width));
            set => SetValue(nameof(Width), value);
        }

        [UiProperty(UiStyleMode.NotSet, UiPropertyFlags.Layout)]
        public UiStyleValue<UnitValue> MinWidth
        {
            get => GetValue<UiStyleValue<UnitValue>>(nameof(MinWidth));
            set => SetValue(nameof(MinWidth), value);
        }

        [UiProperty(UiStyleMode.NotSet, UiPropertyFlags.Layout)]
        public UiStyleValue<UnitValue> MaxWidth
        {
            get => GetValue<UiStyleValue<UnitValue>>(nameof(MaxWidth));
            set => SetValue(nameof(MaxWidth), value);
        }

        [UiProperty(UiStyleMode.Auto, UiPropertyFlags.Layout)]
        public UiStyleValue<UnitValue> Height
        {
            get => GetValue<UiStyleValue<UnitValue>>(nameof(Height));
            set => SetValue(nameof(Height), value);
        }

        [UiProperty(UiStyleMode.NotSet, UiPropertyFlags.Layout)]
        public UiStyleValue<UnitValue> MinHeight
        {
            get => GetValue<UiStyleValue<UnitValue>>(nameof(MinHeight));
            set => SetValue(nameof(MinHeight), value);
        }

        [UiProperty(UiStyleMode.Auto, UiPropertyFlags.Layout)]
        public UiStyleValue<UnitValue> MaxHeight
        {
            get => GetValue<UiStyleValue<UnitValue>>(nameof(MaxHeight));
            set => SetValue(nameof(MaxHeight), value);
        }


        [UiProperty(0f, UiPropertyFlags.Layout)]
        public UiStyleValue<UnitValue> Top
        {
            get => GetValue<UiStyleValue<UnitValue>>(nameof(Top));
            set => SetValue(nameof(Top), value);
        }

        [UiProperty(0f, UiPropertyFlags.Layout)]
        public UiStyleValue<UnitValue> Left
        {
            get => GetValue<UiStyleValue<UnitValue>>(nameof(Left));
            set => SetValue(nameof(Left), value);
        }

        [UiProperty(0f, UiPropertyFlags.Layout)]
        public UiStyleValue<UnitRectValue> Padding
        {
            get => GetValue<UiStyleValue<UnitRectValue>>(nameof(Padding));
            set => SetValue(nameof(Padding), value);
        }

        [UiProperty(0f, UiPropertyFlags.Layout)]
        public UiStyleValue<UnitRectValue> Margin
        {
            get => GetValue<UiStyleValue<UnitRectValue>>(nameof(Margin));
            set => SetValue(nameof(Margin), value);
        }

        [UiProperty(0f, UiPropertyFlags.Layout)]
        public UiStyleValue<BorderRectValue> Border
        {
            get => GetValue<UiStyleValue<BorderRectValue>>(nameof(Border));
            set => SetValue(nameof(Border), value);
        }

        [UiProperty(UiAlignment.Start)]
        public UiStyleValue<UiAlignment> TextAlign
        {
            get => GetValue<UiStyleValue<UiAlignment>>(nameof(TextAlign));
            set => SetValue(nameof(TextAlign), value);
        }

        [UiProperty("1em", UiPropertyFlags.Layout)]
        public UiStyleValue<UnitValue> LineSize
        {
            get => GetValue<UiStyleValue<UnitValue>>(nameof(LineSize));
            set => SetValue(nameof(LineSize), value);
        }

        [UiProperty(UiTextWrap.Whitespaces, UiPropertyFlags.Layout)]
        public UiStyleValue<UiTextWrap> TextWrap
        {
            get => GetValue<UiStyleValue<UiTextWrap>>(nameof(TextWrap));
            set => SetValue(nameof(TextWrap), value);
        }

        [UiProperty(UiVisibility.Visible, UiPropertyFlags.Layout)]
        public UiStyleValue<UiVisibility> Visibility
        {
            get => GetValue<UiStyleValue<UiVisibility>>(nameof(Visibility));
            set => SetValue(nameof(Visibility), value);
        }

        [UiProperty(UiOverflow.Hidden, UiPropertyFlags.Layout)]
        public UiStyleValue<UiOverflow> OverflowX
        {
            get => GetValue<UiStyleValue<UiOverflow>>(nameof(OverflowX));
            set => SetValue(nameof(OverflowX), value);
        }

        [UiProperty(UiOverflow.Hidden, UiPropertyFlags.Layout)]
        public UiStyleValue<UiOverflow> OverflowY
        {
            get => GetValue<UiStyleValue<UiOverflow>>(nameof(OverflowY));
            set => SetValue(nameof(OverflowY), value);
        }

        [UiProperty(UIOrientation.Horizontal, UiPropertyFlags.Layout)]
        public UiStyleValue<UIOrientation> FlexDirection
        {
            get => GetValue<UiStyleValue<UIOrientation>>(nameof(FlexDirection));
            set => SetValue(nameof(FlexDirection), value);
        }

        [UiProperty(0f, UiPropertyFlags.Layout)]
        public UiStyleValue<UnitValue> RowGap
        {
            get => GetValue<UiStyleValue<UnitValue>>(nameof(RowGap));
            set => SetValue(nameof(RowGap), value);
        }

        [UiProperty(0f, UiPropertyFlags.Layout)]
        public UiStyleValue<UnitValue> ColGap
        {
            get => GetValue<UiStyleValue<UnitValue>>(nameof(ColGap));
            set => SetValue(nameof(ColGap), value);
        }

        [UiProperty(UiLayoutType.Flex, UiPropertyFlags.Layout)]
        public UiStyleValue<UiLayoutType> Layout
        {
            get => GetValue<UiStyleValue<UiLayoutType>>(nameof(Layout));
            set => SetValue(nameof(Layout), value);
        }

        [UiProperty(UiWrapMode.NoWrap, UiPropertyFlags.Layout)]
        public UiStyleValue<UiWrapMode> LayoutWrap
        {
            get => GetValue<UiStyleValue<UiWrapMode>>(nameof(LayoutWrap));
            set => SetValue(nameof(LayoutWrap), value);
        }

        [UiProperty(0f, UiPropertyFlags.Layout)]
        public UiStyleValue<float> FlexGrow
        {
            get => GetValue<UiStyleValue<float>>(nameof(FlexGrow));
            set => SetValue(nameof(FlexGrow), value);
        }

        [UiProperty(0f, UiPropertyFlags.Layout)]
        public UiStyleValue<float> FlexShrink
        {
            get => GetValue<UiStyleValue<float>>(nameof(FlexShrink));
            set => SetValue(nameof(FlexShrink), value);
        }

        [UiProperty(0f, UiPropertyFlags.Layout)]
        public UiStyleValue<float> FlexBasis
        {
            get => GetValue<UiStyleValue<float>>(nameof(FlexBasis));
            set => SetValue(nameof(FlexBasis), value);
        }

        [UiProperty(UiStyleMode.NotSet, UiPropertyFlags.Layout)]
        public UiStyleValue<UiAlignment?> AlignSelf
        {
            get => GetValue<UiStyleValue<UiAlignment?>>(nameof(AlignSelf));
            set => SetValue(nameof(AlignSelf), value);
        }

        [UiProperty(UiAlignment.Start, UiPropertyFlags.Layout)]
        public UiStyleValue<UiAlignment> AlignItems
        {
            get => GetValue<UiStyleValue<UiAlignment>>(nameof(AlignItems));
            set => SetValue(nameof(AlignItems), value);
        }

        [UiProperty(UiAlignment.Start, UiPropertyFlags.Layout)]
        public UiStyleValue<UiAlignment> AlignContent
        {
            get => GetValue<UiStyleValue<UiAlignment>>(nameof(AlignContent));
            set => SetValue(nameof(AlignContent), value);
        }

        [UiProperty(UiAlignment.Start, UiPropertyFlags.Layout)]
        public UiStyleValue<UiAlignment> JustifyContent
        {
            get => GetValue<UiStyleValue<UiAlignment>>(nameof(JustifyContent));
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

            if (value is IUiStyleValue styleValue)
            {
                var curStyle = BaseStyle();

                while (styleValue.Mode == UiStyleMode.NotSet)
                {
                    curStyle = curStyle.BaseStyle?.Invoke();

                    if (curStyle == null)
                        return value;

                    styleValue = (IUiStyleValue)curStyle.GetValue<T>(propName)!;
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
