using OpenXr.Framework;
using OpenXr.Framework.Layers;
using Silk.NET.OpenXR;
using System.Diagnostics;
using XrEngine.UI;
using XrMath;

namespace XrEngine.OpenXr
{
    public class XrQuodAttached : Behavior<CanvasView3D>, IDisposable
    {
        XrTextureQuadLayer[]? _layers;
        readonly XrApp _app;

        public XrQuodAttached(XrApp app)
        {
            _app = app;
        }

        public void Dispose()
        {
            if (_app == null || _layers == null)
                return;

            foreach (var layer in _layers)
            {
                _app.Layers.List.Remove(layer);
                layer.Dispose();
            }

            _layers = null;

            GC.SuppressFinalize(this);
        }

        protected unsafe override void OnAttach()
        {
            Debug.Assert(_host != null);

            _host.EnableDepthCull = _app.RenderOptions.UseQuodDepthCull && (
                     _app.RenderOptions.SampleCount <= 1 ||
                     !XrPlatform.IsAndroid ||
                     _app.RenderOptions.RenderMode != XrRenderMode.MultiView);

            if (_host.EnableDepthCull || _host.IsStereo)
            {
                _layers = _app.Layers.AddStereoQuod(_host.BindToQuad(), RenderQuod, _host.PixelSize, XrLayerPriority.UiQuods);
            }
            else
            {
                var layer = new XrTextureQuadLayer(_host.BindToQuad(), RenderQuod, _host.PixelSize)
                {
                    Priority = XrLayerPriority.UiQuods
                };

                _app.Layers.Add(layer);

                _layers = [layer];
            }
        }

        protected override void Update(RenderContext ctx)
        {
            Debug.Assert(_host != null);

            if (_app.IsStarted && _host.Mode == CanvasViewMode.Texture)
                _host.Mode = CanvasViewMode.RenderTarget;

            if (!_app.IsStarted && _host.Mode == CanvasViewMode.RenderTarget)
                _host.Mode = CanvasViewMode.Texture;

        }

        unsafe bool RenderQuod(SwapchainImageBaseHeader* image, Size2I size, long predTime, int eye)
        {
            Debug.Assert(_host != null);

            if (image == null)
                return _host.EnableDepthCull || _host.NeedDraw;

            //TODO handle vulkan
            var glImage = (SwapchainImageOpenGLKHR*)image;

            _host.SetRenderTarget(glImage->Image, size.Width, size.Height, eye);
            _host.Draw(EngineApp.Current.RenderContext);

            return true;
        }

        public XrTextureQuadLayer[]? Layers => _layers;

    }
}
