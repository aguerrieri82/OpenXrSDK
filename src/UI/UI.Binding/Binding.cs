namespace UI.Binding
{
    public enum BindingMode
    {

    }

    public class Binding : IDisposable
    {
        public Binding(IProperty source, IProperty dest, BindingMode mode)
        {
            Source = source;
            Dest = dest;
            Mode = mode;

            Source.Changed += OnSourceChanged;
            Dest.Changed += OnDestChanged;
        }

        private void OnDestChanged(object? sender, EventArgs e)
        {
            var srcValue = Source.Value;
            var dstValue = Dest.Value;
            if (!Equals(srcValue, dstValue))
                Source.Value = dstValue;
        }

        private void OnSourceChanged(object? sender, EventArgs e)
        {
            var srcValue = Source.Value;
            var dstValue = Dest.Value;
            if (!Equals(srcValue, dstValue))
                Dest.Value = srcValue;
        }

        public void Dispose()
        {
            Source.Changed -= OnSourceChanged;
            Dest.Changed -= OnDestChanged;
            GC.SuppressFinalize(this);
        }

        public BindingMode Mode;

        public IProperty Source;

        public IProperty Dest;

    }
}
