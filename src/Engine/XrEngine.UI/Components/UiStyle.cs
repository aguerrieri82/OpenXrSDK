using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine.UI.Components;

namespace XrEngine.UI
{

    public class UiStyle : UiObject
    {
        UiComponent _owner;

        public UiStyle(UiComponent owner)
        {
            _owner = owner;
        }

        static UiProperty<UiStyleValue<T>> CreateStyleProp<T>(string name, UiStyleMode mode = UiStyleMode.Inherit)
        {
            var prop = CreateProp<UiStyleValue<T>>(name, typeof(UiStyle));
            prop.DefaultValue = new UiStyleValue<T> { Mode = mode };    
            return prop;
        }

        static UiProperty<UiStyleValue<T>> CreateStyleProp<T>(string name, T? defaultValue)
        {
            var prop = CreateProp<UiStyleValue<T>>(name, typeof(UiStyle));
            prop.DefaultValue = (UiStyleValue<T>)defaultValue;
            return prop;
        }

        static readonly UiProperty<UiStyleValue<Color?>> BackgroundColorProp = CreateStyleProp<Color?>(nameof(BackgroundColor), UiStyleMode.NotSet);

        static readonly UiProperty<UiStyleValue<Color?>> ColorProp = CreateStyleProp<Color?>(nameof(Color));

        static readonly UiProperty<UiStyleValue<UnitValue>> FontSizeProp = CreateStyleProp<UnitValue>(nameof(FontSize));

        static readonly UiProperty<UiStyleValue<string>> FontFamilyProp = CreateStyleProp(nameof(FontFamily), "Sans Serif");

        static readonly UiProperty<UiStyleValue<UnitValue>> WidthProp = CreateStyleProp<UnitValue>(nameof(Width), UiStyleMode.Auto);

        static readonly UiProperty<UiStyleValue<UnitValue>> HeightProp = CreateStyleProp<UnitValue>(nameof(Height), UiStyleMode.Auto);

        static readonly UiProperty<UiStyleValue<UnitValue>> TopProp = CreateStyleProp<UnitValue>(nameof(Top), UiStyleMode.NotSet);

        static readonly UiProperty<UiStyleValue<UnitValue>> LeftProp = CreateStyleProp<UnitValue>(nameof(Left), UiStyleMode.NotSet);

        static readonly UiProperty<UiStyleValue<UnitRectValue>> PaddingProp = CreateStyleProp<UnitRectValue>(nameof(Padding), UnitRectValue.All(0));

        static readonly UiProperty<UiStyleValue<UnitRectValue>> MarginProp = CreateStyleProp<UnitRectValue>(nameof(Margin), UnitRectValue.All(0));


        internal UiStyleValue<T> GetStyleValue<T>(UiProperty<UiStyleValue<T>> prop)
        {
            var value = GetValue(prop);
            value._property = prop;
            return value;
        }


        protected override void OnPropertyChanged<T>(UiProperty<T> prop, T? value, T? oldValue) where T : default
        {
            _owner.OnStyleChanged(prop, value, oldValue);
        }


        public UiStyleValue<Color?> BackgroundColor
        {
            get => GetStyleValue(BackgroundColorProp);
            set => SetValue(BackgroundColorProp, value);
        }

        public UiStyleValue<Color?> Color
        {
            get => GetStyleValue(ColorProp);
            set => SetValue(ColorProp, value);
        }

        public UiStyleValue<UnitValue> FontSize
        {
            get => GetStyleValue(FontSizeProp);
            set => SetValue(FontSizeProp, value);
        }

        public UiStyleValue<string> FontFamily
        {
            get => GetStyleValue(FontFamilyProp);
            set => SetValue(FontFamilyProp, value);
        }

        public UiStyleValue<UnitValue> Width
        {
            get => GetStyleValue(WidthProp);
            set => SetValue(WidthProp, value);
        }


        public UiStyleValue<UnitValue> Height
        {
            get => GetStyleValue(HeightProp);
            set => SetValue(HeightProp, value);
        }


        public UiStyleValue<UnitValue> Top
        {
            get => GetStyleValue(TopProp);
            set => SetValue(LeftProp, value);
        }

        public UiStyleValue<UnitValue> Left
        {
            get => GetStyleValue(TopProp);
            set => SetValue(LeftProp, value);
        }

        public UiStyleValue<UnitRectValue> Padding
        {
            get => GetStyleValue(PaddingProp);
            set => SetValue(PaddingProp, value);
        }

        public UiStyleValue<UnitRectValue> Margin
        {
            get => GetStyleValue(MarginProp);
            set => SetValue(MarginProp, value);
        }
    }
}
