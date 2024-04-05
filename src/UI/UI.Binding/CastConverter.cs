namespace UI.Binding
{
    public struct CastConverter<TSrc, TDst> : IValueConverter<TSrc, TDst>
    {
        public readonly TSrc ConvertFrom(TDst value)
        {
            return (TSrc)(object)value!;
        }

        public readonly TDst ConvertTo(TSrc value)
        {
            return (TDst)(object)value!;
        }
    }
}
