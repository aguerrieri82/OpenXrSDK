using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using XrMath;

namespace XrEditor
{
    public class ColorToHex : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
                value = XrMath.Color.Parse(str);

            if (value is XrMath.Color color)
            {
                var res = System.Windows.Media.Color.FromArgb((byte)(color.A * 255), (byte)(color.R * 255), (byte)(color.G * 255), (byte)(color.B * 255));
                if (typeof(Brush).IsAssignableFrom(targetType))
                    return new SolidColorBrush(res);
                return res;
            }

            throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush solidColor)
                value = solidColor.Color;

            if (value is System.Windows.Media.Color color)
                return new XrMath.Color(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f).ToHex();
            throw new NotSupportedException();
        }
    }
}
