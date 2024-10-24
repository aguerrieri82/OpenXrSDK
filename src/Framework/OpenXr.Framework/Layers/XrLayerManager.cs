﻿using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public unsafe class XrLayerManager : IDisposable
    {
        protected List<IXrLayer> _layers = [];
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

        public CompositionLayerBaseHeader*[] Render(ref View[] views, Space space, long predTime, out uint layerCount)
        {
            if (_layersPointers == null || _layersPointers.Length != _layers.Count)
                _layersPointers = new CompositionLayerBaseHeader*[_layers.Count];

            layerCount = 0;

            foreach (var layer in _layers)
            {
                if (!layer.IsEnabled)
                    continue;

                bool result = layer.Update(ref views, predTime);
                if (result)
                {
                    _layersPointers[layerCount] = layer.Header;

                    if ((layer.Flags & XrLayerFlags.EmptySpace) == 0)
                        layer.Header->Space = space;

                    layerCount++;
                }
            }

            return _layersPointers;
        }

        public T Add<T>() where T : IXrLayer, new()
        {
            return Add(new T());
        }

        public T Add<T>(T layer) where T : IXrLayer
        {
            _layers.Add(layer);
            return layer;
        }

        public void Dispose()
        {
            foreach (var layer in _layers)
                layer.Dispose();

            _layers.Clear();

            GC.SuppressFinalize(this);
        }

        public IList<IXrLayer> List => _layers;
    }
}
