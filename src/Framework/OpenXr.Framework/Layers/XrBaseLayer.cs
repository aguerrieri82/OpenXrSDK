using Silk.NET.OpenXR;
using System.Runtime.InteropServices;

namespace OpenXr.Framework
{
    public unsafe abstract class XrBaseLayer<T> : IXrLayer where T : unmanaged
    {
        protected T* _header;
        protected XrApp? _xrApp;
        private bool _isEnabled;

        public XrBaseLayer()
        {
            _header = (T*)Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(T)));
            (*_header) = new T();
            _isEnabled = true;
        }

        public virtual void Create()
        {

        }

        public virtual void Initialize(XrApp app, IList<string> extensions)
        {
            _xrApp = app;
        }

        protected virtual void OnEnabledChanged(bool isEnabled)
        {

        }

        public virtual void Dispose()
        {
            if (_header != null)
            {
                Marshal.FreeHGlobal(new nint(_header));
                _header = null;
            }
        }

        public bool Update(ref View[] views, XrSwapchainInfo[] swapchains, long predTime)
        {
            var span = new Span<T>(_header, 1);
            return Update(ref span[0], ref views, swapchains, predTime);
        }

        protected abstract bool Update(ref T layer, ref View[] views, XrSwapchainInfo[] swapchains, long predTime);

        public virtual void OnBeginFrame()
        {

        }

        public virtual void OnEndFrame()
        {

        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value)
                    return;
                _isEnabled = value;
                OnEnabledChanged(_isEnabled);
            }
        }

        public virtual XrLayerFlags Flags => XrLayerFlags.None;

        public CompositionLayerBaseHeader* Header => (CompositionLayerBaseHeader*)_header;
    }
}
