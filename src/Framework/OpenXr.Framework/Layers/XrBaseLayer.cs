using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public unsafe abstract class XrBaseLayer<T> : IXrLayer where T : unmanaged
    {
        protected NativeStruct<T> _header;
        protected XrApp? _xrApp;
        protected bool _isEnabled;

        public XrBaseLayer()
        {
            _header.Value = new T();
            _isEnabled = true;
        }

        public virtual void Create()
        {

        }

        public virtual void Destroy()
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
            Destroy();
            _header.Dispose();
        }

        public bool Update(ref View[] views, long predTime)
        {
            var span = new Span<T>(_header.Pointer, 1);
            return Update(ref span[0], ref views, predTime);
        }

        protected abstract bool Update(ref T layer, ref View[] views, long displayTime);

        public virtual void OnBeginFrame(Space space, long displayTime)
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

        public int Priority { get; set; }

        public virtual XrLayerFlags Flags => XrLayerFlags.None;

        public CompositionLayerBaseHeader* Header => (CompositionLayerBaseHeader*)_header.Pointer;
    }
}
