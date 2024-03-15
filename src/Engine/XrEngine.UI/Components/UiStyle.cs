using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine.UI.Components;

namespace XrEngine.UI
{

    public class UiStyle : UiObject
    {

        protected UiComponent _owner;

        public UiStyle(UiComponent owner)
        {
            _owner = owner;
        }

        protected override void OnPropertyChanged(string propName, object? value, object? oldValue)
        {
            _owner.OnStyleChanged(propName, (IUiStyleValue)value!, (IUiStyleValue)oldValue!);
        }

        [DefaultValue(UiStyleMode.NotSet)]
        public UiStyleValue<Color?> BackgroundColor
        {
            get => GetValue<UiStyleValue<Color?>>(nameof(BackgroundColor));
            set => SetValue(nameof(BackgroundColor), value);
        }

        [DefaultValue(UiStyleMode.Inherit)]
        public UiStyleValue<Color?> Color
        {
            get => GetValue<UiStyleValue<Color?>>(nameof(Color));
            set => SetValue(nameof(Color), value);
        }

        [DefaultValue(UiStyleMode.Inherit)]
        public UiStyleValue<UnitValue> FontSize
        {
            get => GetValue<UiStyleValue<UnitValue>>(nameof(FontSize));
            set => SetValue(nameof(FontSize), value);
        }

        [DefaultValue(UiStyleMode.Inherit)]
        public UiStyleValue<string> FontFamily
        {
            get => GetValue<UiStyleValue<string>>(nameof(FontFamily));
            set => SetValue(nameof(FontFamily), value);
        }

        [DefaultValue(UiStyleMode.Auto)]
        public UiStyleValue<UnitValue> Width
        {
            get => GetValue<UiStyleValue<UnitValue>>(nameof(Width));
            set => SetValue(nameof(Width), value);
        }

        [DefaultValue(UiStyleMode.Auto)]
        public UiStyleValue<UnitValue> Height
        {
            get => GetValue<UiStyleValue<UnitValue>>(nameof(Height));
            set => SetValue(nameof(Height), value);
        }

        [DefaultValue(0f)]
        public UiStyleValue<UnitValue> Top
        {
            get => GetValue<UiStyleValue<UnitValue>>(nameof(Top));
            set => SetValue(nameof(Top), value);
        }

        [DefaultValue(0f)]
        public UiStyleValue<UnitValue> Left
        {
            get => GetValue<UiStyleValue<UnitValue>>(nameof(Left));
            set => SetValue(nameof(Left), value);
        }

        [DefaultValue(0f)]
        public UiStyleValue<UnitRectValue> Padding
        {
            get => GetValue<UiStyleValue<UnitRectValue>>(nameof(Padding));
            set => SetValue(nameof(Padding), value);
        }

        [DefaultValue(0f)]
        public UiStyleValue<UnitRectValue> Margin
        {
            get => GetValue<UiStyleValue<UnitRectValue>>(nameof(Margin));
            set => SetValue(nameof(Margin), value);
        }

        [DefaultValue(0f)]
        public UiStyleValue<BorderRectValue> Border
        {
            get => GetValue<UiStyleValue<BorderRectValue>>(nameof(Border));
            set => SetValue(nameof(Border), value);
        }

        [DefaultValue(UiAlignment.Start)]
        public UiStyleValue<UiAlignment> TextAlign
        {
            get => GetValue<UiStyleValue<UiAlignment>>(nameof(TextAlign));
            set => SetValue(nameof(TextAlign), value);
        }

        [DefaultValue("1em")]
        public UiStyleValue<UnitValue> LineSize
        {
            get => GetValue<UiStyleValue<UnitValue>>(nameof(LineSize));
            set => SetValue(nameof(LineSize), value);
        }

        [DefaultValue(UiTextWrap.Whitespaces)]
        public UiStyleValue<UiTextWrap> TextWrap
        {
            get => GetValue<UiStyleValue<UiTextWrap>>(nameof(TextWrap));
            set => SetValue(nameof(TextWrap), value);
        }

        [DefaultValue(UiVisibility.Visible)]
        public UiStyleValue<UiVisibility> Visibility
        {
            get => GetValue<UiStyleValue<UiVisibility>>(nameof(Visibility));
            set => SetValue(nameof(Visibility), value);
        }

        [DefaultValue(UiOverflow.Hidden)]
        public UiStyleValue<UiOverflow> OverflowX
        {
            get => GetValue<UiStyleValue<UiOverflow>>(nameof(OverflowX));
            set => SetValue(nameof(OverflowX), value);
        }

        [DefaultValue(UiOverflow.Hidden)]
        public UiStyleValue<UiOverflow> OverflowY
        {
            get => GetValue<UiStyleValue<UiOverflow>>(nameof(OverflowY));
            set => SetValue(nameof(OverflowY), value);
        }

        [DefaultValue(UIOrientation.Horizontal)]
        public UiStyleValue<UIOrientation> FlexDirection
        {
            get => GetValue<UiStyleValue<UIOrientation>>(nameof(FlexDirection));
            set => SetValue(nameof(FlexDirection), value);
        }

        [DefaultValue(0f)]
        public UiStyleValue<UnitValue> RowGap
        {
            get => GetValue<UiStyleValue<UnitValue>>(nameof(RowGap));
            set => SetValue(nameof(RowGap), value);
        }

        [DefaultValue(0f)]
        public UiStyleValue<UnitValue> ColGap
        {
            get => GetValue<UiStyleValue<UnitValue>>(nameof(ColGap));
            set => SetValue(nameof(ColGap), value);
        }

        [DefaultValue(UiLayoutType.Flex)]
        public UiStyleValue<UiLayoutType> Layout
        {
            get => GetValue<UiStyleValue<UiLayoutType>>(nameof(Layout));
            set => SetValue(nameof(Layout), value);
        }

        [DefaultValue(UiWrapMode.NoWrap)]
        public UiStyleValue<UiWrapMode> LayoutWrap
        {
            get => GetValue<UiStyleValue<UiWrapMode>>(nameof(LayoutWrap));
            set => SetValue(nameof(LayoutWrap), value);
        }

        [DefaultValue(0f)]
        public UiStyleValue<UnitValue> FlexGrow
        {
            get => GetValue<UiStyleValue<UnitValue>>(nameof(FlexGrow));
            set => SetValue(nameof(FlexGrow), value);
        }

        [DefaultValue(0f)]
        public UiStyleValue<UnitValue> FlexShrink
        {
            get => GetValue<UiStyleValue<UnitValue>>(nameof(FlexShrink));
            set => SetValue(nameof(FlexShrink), value);
        }

        [DefaultValue(0f)]
        public UiStyleValue<UnitValue> FlexBasis
        {
            get => GetValue<UiStyleValue<UnitValue>>(nameof(FlexBasis));
            set => SetValue(nameof(FlexBasis), value);
        }

        [DefaultValue(UiStyleMode.NotSet)]
        public UiStyleValue<UiAlignment> AlignSelf
        {
            get => GetValue<UiStyleValue<UiAlignment>>(nameof(AlignSelf));
            set => SetValue(nameof(AlignSelf), value);
        }

        [DefaultValue(UiAlignment.Start)]
        public UiStyleValue<UiAlignment> AlignItems
        {
            get => GetValue<UiStyleValue<UiAlignment>>(nameof(AlignItems));
            set => SetValue(nameof(AlignItems), value);
        }

        [DefaultValue(UiAlignment.Start)]
        public UiStyleValue<UiAlignment> AlignContent
        {
            get => GetValue<UiStyleValue<UiAlignment>>(nameof(AlignContent));
            set => SetValue(nameof(AlignContent), value);
        }

        [DefaultValue(UiAlignment.Start)]
        public UiStyleValue<UiAlignment> JustifyContent
        {
            get => GetValue<UiStyleValue<UiAlignment>>(nameof(JustifyContent));
            set => SetValue(nameof(JustifyContent), value);
        }

        public Func<UiStyle>? BaseStyle { get; set; }

        public UiComponent Owner => _owner;
    }


    public class UIActualStyle : UiStyle
    {
        public UIActualStyle(UiComponent owner)
            : base(owner)
        {
        }

        public override T? GetValue<T>(string propName) where T : default
        {
            var parent = _owner.Parent ?? _owner.Host;

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
                        break;

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
