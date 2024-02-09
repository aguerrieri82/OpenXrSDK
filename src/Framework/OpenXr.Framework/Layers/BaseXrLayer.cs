using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework
{
    public unsafe abstract class BaseXrLayer<T> : IXrLayer where T : struct
    {
        protected T* _header;
        protected XrApp _xrApp;

        public BaseXrLayer(XrApp xrApp)
        {
            _header = (T*)Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(T)));
            (*_header) = new T();   
            _xrApp = xrApp;
            IsEnabled = true;

        }
        public virtual void Dispose()
        {
            if (_header!= null)
            {
                Marshal.FreeHGlobal(new nint(_header));
                _header = null;
            }
        }

        public bool Render(ref View[] views, XrSwapchainInfo[] swapchains)
        {
            var span = new Span<T>(_header, 1);
            return Render(ref span[0], ref views, swapchains);
        }

        protected abstract bool Render(ref T layer, ref View[] views, XrSwapchainInfo[] swapchains);

        public bool IsEnabled {  get; set; }

        public CompositionLayerBaseHeader* Header => (CompositionLayerBaseHeader*)_header;
    }
}
