using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework
{
    public unsafe class XrLayerManager : IDisposable
    {
        protected List<IXrLayer> _layers = new();
        protected XrApp _xrApp;
        protected CompositionLayerBaseHeader*[]? _layersPointers;

        public XrLayerManager(XrApp xrApp)
        {
            _xrApp = xrApp; 
        }

        public void AddProjection(RenderViewDelegate renderView)
        {
            _layers.Add(new XrProjectionLayer(_xrApp, renderView));
        }

        public CompositionLayerBaseHeader*[] Render(ref View[] views, XrSwapchainInfo[] swapchains, long predTime, out uint layerCount)
        {
            if (_layersPointers == null || _layersPointers.Length != _layers.Count)
                _layersPointers = new CompositionLayerBaseHeader*[_layers.Count];

            layerCount = 0;

            foreach (var layer in _layers)
            {
                if (!layer.IsEnabled)
                    continue;

                bool result = layer.Render(ref views, swapchains, predTime);
                if (result)
                {
                    _layersPointers[layerCount] = layer.Header;
                    layerCount++;
                }
            }

            return _layersPointers;
        }

        public void Dispose()
        {
            foreach (var layer in _layers)
                layer.Dispose();

            _layers.Clear();
        }

        public IList<IXrLayer> Layers => _layers;
    }
}
