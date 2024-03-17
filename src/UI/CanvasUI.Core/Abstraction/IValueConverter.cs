using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanvasUI
{
    public interface IValueConverter
    {
        bool CanConvert(Type srcType, Type dstType);

        object? ConvertFrom(object? value);

        object? ConvertTo(object? value);
    }

    public interface IValueConverter<TSrc, TDst> : IValueConverter
    {
        TSrc ConvertFrom(TDst value);   

        TDst ConvertTo(TSrc value);

        bool IValueConverter.CanConvert(Type srcType, Type dstType) =>
                typeof(TSrc).IsAssignableFrom(srcType) &&
                typeof(TDst).IsAssignableFrom(dstType);

        object ? IValueConverter.ConvertFrom(object? value) => ConvertFrom((TDst)value!);

        object? IValueConverter.ConvertTo(object? value) => ConvertTo((TSrc)value!);
    }
}
