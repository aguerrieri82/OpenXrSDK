using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public unsafe class XrLayerManager : IDisposable
    {
        protected struct PendingOperation
        {
            public IXrLayer Layer;
            public bool Add;
            public bool Head;
        }

        protected readonly List<IXrLayer> _layers = [];
        protected readonly List<PendingOperation> _pending = [];

        protected XrApp _xrApp;
        protected CompositionLayerBaseHeader*[]? _layersPointers;

        public XrLayerManager(XrApp xrApp)
        {
            _xrApp = xrApp;
        }

        public void Commit()
        {
            _layers.Sort((a, b) => a.Priority - b.Priority);
        }

        protected void ApplyPending()
        {
            if (_pending.Count == 0)
                return;

            foreach (var operation in _pending)
            {
                if (operation.Add)
                {
                    if (operation.Head)
                        _layers.Insert(0, operation.Layer);
                    else
                        _layers.Add(operation.Layer);

                    operation.Layer.Initialize(_xrApp, []);
                    operation.Layer.Create();
                }
                else
                    _layers.Remove(operation.Layer);
            }

            _pending.Clear();

            Commit();
        }

        public CompositionLayerBaseHeader*[] Render(ref View[] views, Space space, long predTime, out uint layerCount)
        {
            ApplyPending();

            if (_layersPointers == null || _layersPointers.Length != _layers.Count)
                _layersPointers = new CompositionLayerBaseHeader*[_layers.Count];

            layerCount = 0;

            for (var i = 0; i < _layers.Count; i++)
            {
                var layer = _layers[i];

                if (!layer.IsEnabled)
                    continue;

                if (!layer.Update(ref views, predTime))
                    continue;

                _layersPointers[layerCount] = layer.Header;

                if ((layer.Flags & XrLayerFlags.EmptySpace) == 0)
                    layer.Header->Space = space;

                layerCount++;
            }

            return _layersPointers;
        }

        public T Add<T>() where T : IXrLayer, new()
        {
            return Add(new T());
        }

        public T Add<T>(T layer) where T : IXrLayer
        {
            if (_xrApp.IsStarted)
            {
                _pending.Add(new PendingOperation
                {
                    Layer = layer,
                    Add = true
                });
            }
            else
                _layers.Add(layer);

            return layer;
        }

        public T AddHead<T>(T layer) where T : IXrLayer
        {
            if (_xrApp.IsStarted)
            {
                _pending.Add(new PendingOperation
                {
                    Layer = layer,
                    Add = true,
                    Head = true
                });
            }
            else
                _layers.Insert(0, layer);

            return layer;
        }

        public void Remove(IXrLayer layer)
        {
            if (_xrApp.IsStarted)
            {
                _pending.Add(new PendingOperation
                {
                    Layer = layer
                });

                return;
            }

            _layers.Remove(layer);
        }

        public void Dispose()
        {
            foreach (var layer in _layers)
                layer.Dispose();

            foreach (var operation in _pending)
            {
                if (operation.Add)
                    operation.Layer.Dispose();
            }

            _layers.Clear();
            _pending.Clear();

            GC.SuppressFinalize(this);
        }

        public IReadOnlyList<IXrLayer> List => _layers;
    }
}