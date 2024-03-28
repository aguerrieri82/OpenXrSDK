using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.Binding
{
    public class ConvertProperty<TSrc, TDst> : IProperty<TDst>
    {
        IValueConverter<TSrc, TDst> _converter;
        IProperty<TSrc> _srcProp;

        public ConvertProperty(IProperty<TSrc> srcProp, IValueConverter<TSrc, TDst> converter)
        {
            _converter = converter;
            _srcProp = srcProp;
        }


        public TDst Value
        {
            get => _converter.ConvertTo(_srcProp.Value);
            set => _srcProp.Value = _converter.ConvertFrom(value);
        }

        public string? Name => _srcProp.Name;

        public event EventHandler Changed
        {
            add { _srcProp.Changed += value; }

            remove { _srcProp.Changed -= value; }
        }
    }
}
