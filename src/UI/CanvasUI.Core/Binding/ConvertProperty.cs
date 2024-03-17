using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanvasUI
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

        public TDst Get()
        {
            throw new NotImplementedException();
        }

        public void Set(TDst value)
        {
            throw new NotImplementedException();
        }

        public string? Name => _srcProp.Name;

        public event EventHandler Changed;

    }
}
