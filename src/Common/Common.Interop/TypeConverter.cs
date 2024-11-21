
namespace Common.Interop
{
    public static unsafe class TypeConverter
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

    public readonly unsafe ref struct Converter<TIn> where TIn : unmanaged
    {
        readonly ref TIn _value;

        public Converter(ref TIn value)
        {
            _value = ref value;
        }

        public TOut To<TOut>()
        {
            if (sizeof(TIn) < sizeof(TOut))
                throw new ArgumentException();

            fixed (TIn* pValue = &_value)
                return *(TOut*)pValue;
        }
    }

    public readonly unsafe ref struct ArrayConverter<TIn> where TIn : unmanaged
    {
        readonly TIn[] _value;

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
