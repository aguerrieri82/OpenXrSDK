using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Common.Interop
{
    public static class TypeConverter
    {
        public static Converter<TIn> Convert<TIn>(this ref TIn value) where TIn : unmanaged
        {
            return new Converter<TIn>(ref value);
        }

        public static ArrayConverter<TIn> Convert<TIn>(this TIn[] value) where TIn : unmanaged
        {
            return new ArrayConverter<TIn>(value);
        }
    }

    public unsafe readonly ref struct Converter<TIn> where TIn : unmanaged
    {
        readonly ref TIn _value;

        public Converter(ref TIn value)
        {
            _value = ref value;
        }


        public TOut To<TOut>() where TOut : unmanaged
        {
            if (sizeof(TIn) < sizeof(TOut))
                throw new ArgumentException($"Target {typeof(TOut).Name} is larger than Source {typeof(TIn).Name}");

            return Unsafe.As<TIn, TOut>(ref _value);
        }


        public ref TOut AsRef<TOut>() where TOut : unmanaged
        {
            if (sizeof(TIn) < sizeof(TOut))
                throw new ArgumentException($"Target {typeof(TOut).Name} is larger than Source {typeof(TIn).Name}");

            return ref Unsafe.As<TIn, TOut>(ref _value);
        }
    }

    public unsafe readonly ref struct ArrayConverter<TIn> where TIn : unmanaged
    {
        readonly TIn[] _value;

        public ArrayConverter(TIn[] value)
        {
            _value = value;
        }

        public ReadOnlySpan<TOut> To<TOut>() where TOut : unmanaged
        {

            if (sizeof(TIn) != sizeof(TOut))
                throw new ArgumentException("Types must be same size for array conversion");

            return MemoryMarshal.Cast<TIn, TOut>(_value);
        }
    }
}