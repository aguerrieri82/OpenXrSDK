using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanvasUI
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
            var srcValue = Source.Get();
            var dstValue = Dest.Get();
            if (!Equals(srcValue, dstValue))
                Source.Set(dstValue);
        }

        private void OnSourceChanged(object? sender, EventArgs e)
        {
            var srcValue = Source.Get();
            var dstValue = Dest.Get();
            if (!Equals(srcValue, dstValue))
                Dest.Set(srcValue);
        }

        public void Dispose()
        {
            Source.Changed -= OnSourceChanged;
            Dest.Changed -= OnDestChanged;
        }

        public BindingMode Mode;

        public IProperty Source;

        public IProperty Dest;

    }
}
