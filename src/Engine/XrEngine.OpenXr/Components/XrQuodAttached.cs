using OpenXr.Framework;
using OpenXr.Framework.Angle;
using OpenXr.Framework.Layers;
using Silk.NET.OpenXR;
using System.Diagnostics;
using XrEngine.OpenGL;
using XrEngine.UI;

namespace XrEngine.OpenXr
{
    public class XrQuodAttached : BaseXrComponent<CanvasView3D>, IDisposable
    {
        XrQuadLayer? _layer;
        AngleVulkanContext? _vulkanCtx;

        public XrQuodAttached(XrApp app)
        {
            _xrApp = app;
        }

        public void Dispose()
        {
            if (_layer != null)
            {
                _xrApp?.Layers.Remove(_layer);
                _layer.Dispose();
                _xrApp = null;
                _layer = null;
            }

            GC.SuppressFinalize(this);
        }


        protected override void DetachXr()
        {
            _host.Mode = CanvasViewMode.Texture;
        }

        protected unsafe override void AttachXr()
        {
            Debug.Assert(_host != null);

            _host.Mode = CanvasViewMode.RenderTarget;

            var useAngle = OpenGLRender.Current!.Features.IsAngle;

            _layer = _xrApp!.Layers.AddQuod(_host.BindToQuad(), RenderQuod, _host.PixelSize, XrLayerPriority.UiGeomeytry);
        }

        unsafe bool RenderQuod(GeometryRenderData data, SwapchainImageBaseHeader* image, long predTime)
        {
            Debug.Assert(_host != null);

            if (image == null)
                return _host.NeedDraw;

            uint glImage;

            var useAngle = OpenGLRender.Current!.Features.IsAngle;

            OpenGLRender.Current.PushGroup("Render Quad");

            var swapchain = data.Swapchain!;

            if (useAngle)
            {
                _vulkanCtx ??= Context.Require<AngleVulkanContext>();

                glImage = _vulkanCtx.AttachVulkanImage(image, swapchain).Texture;
            }
            else
                glImage = ((SwapchainImageOpenGLKHR*)image)->Image;

            _host.SetRenderTarget(glImage, (uint)swapchain.Size.Width, (uint)swapchain.Size.Height, data.Eye);
            _host.Draw(EngineApp.Current.RenderContext);

            OpenGLRender.Current.PopGroup();

            return true;
        }

        public XrQuadLayer? Layer => _layer;

    }
}
