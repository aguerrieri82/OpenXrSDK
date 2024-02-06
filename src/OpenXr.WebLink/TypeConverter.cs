using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS8500

namespace OpenXr.WebLink
{
    public static unsafe class TypeConverter
    {
        public static Converter<TIn> Convert<TIn>(this TIn value) where TIn : struct
        {
            return new Converter<TIn>(value);
        }

        public static ArrayConverter<TIn> Convert<TIn>(this TIn[] value) where TIn : struct
        {
            return new ArrayConverter<TIn>(value);
        }
    }

    public unsafe struct Converter<TIn> where TIn : struct
    {
        TIn _value;

        public Converter(TIn value) 
        {
            _value = value;
        } 

        public TOut To<TOut>()
        {
            if (sizeof(TIn) != sizeof(TOut))
                throw new ArgumentException();

            fixed (TIn* pValue = &_value)
                return *(TOut*)pValue;
        }
    }

    public unsafe struct ArrayConverter<TIn> where TIn : struct
    {
        TIn[] _value;

        public ArrayConverter(TIn[] value)
        {
            _value = value;
        }

        public TOut[] To<TOut>()
        {
            if (sizeof(TIn) != sizeof(TOut))
                throw new ArgumentException();

            fixed (TIn* pValue = _value)
                return new Span<TOut>((TOut*)pValue, _value.Length).ToArray();
        }
    }
}
