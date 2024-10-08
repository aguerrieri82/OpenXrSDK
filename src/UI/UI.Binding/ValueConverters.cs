﻿using System.Globalization;

namespace UI.Binding
{
    public class FloatStringConverter : IValueConverter<float, string>
    {
        public float ConvertFrom(string value)
        {
            return float.Parse(value, CultureInfo.InvariantCulture);
        }

        public string ConvertTo(float value)
        {
            return value.ToString(null, CultureInfo.InvariantCulture);
        }
    }

    public static class ValueConverters
    {
        static readonly List<IValueConverter> _converters = [];

        static ValueConverters()
        {
            Register(new FloatStringConverter());
        }

        public static void Register(IValueConverter converter)
        {
            _converters.Add(converter);
        }


        public static IValueConverter<TSrc, TDst> Get<TSrc, TDst>()
        {
            var result = _converters.OfType<IValueConverter<TSrc, TDst>>().FirstOrDefault();
            if (result == null)
                throw new InvalidCastException();
            return result;
        }
    }
}
