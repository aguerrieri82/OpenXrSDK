#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using OpenXr.Framework;
using OpenXr.Framework.Angle;
using OpenXr.Framework.Layers;
using Silk.NET.OpenXR;
using Silk.NET.Vulkan;
using System.Diagnostics;
using XrEngine.OpenGL;
using XrEngine.UI;


namespace XrEngine.OpenXr
{
    public class XrQuodAttached : Behavior<CanvasView3D>, IDisposable
    {
        XrTextureQuadLayer[]? _layers;
        AngleVulkanContext? _vulkanCtx;
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
                bool useAngle = OpenGLRender.Current!.UseAngle;

                var layer = new XrTextureQuadLayer(_host.BindToQuad(), RenderQuod, _host.PixelSize)
                {
                    Priority = XrLayerPriority.UiQuods,
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

        unsafe bool RenderQuod(QuadData data, SwapchainImageBaseHeader* image,  long predTime)
        {
            Debug.Assert(_host != null);

            if (image == null)
                return _host.EnableDepthCull || _host.NeedDraw;

            uint glImage;

            bool useAngle = OpenGLRender.Current!.UseAngle;

            OpenGLRender.Current.PushGroup("Render Quad");

            if (useAngle)
            {
                _vulkanCtx ??= Context.Require<AngleVulkanContext>();
                
                var vkImage = (nint)((SwapchainImageVulkanKHR*)image)->Image;

                glImage = _vulkanCtx.AttachVulkanImage(
                    vkImage,
                    data.Format,
                    (uint)data.Size.Width,
                    (uint)data.Size.Height,
                    1, 1, 1,
                    ImageUsageFlags.SampledBit |
                    ImageUsageFlags.ColorAttachmentBit |
                    ImageUsageFlags.TransferDstBit |
                    ImageUsageFlags.TransferSrcBit |
                    ImageUsageFlags.InputAttachmentBit,
                    ImageCreateFlags.None,
                    TextureTarget.Texture2D).Texture;

                _vulkanCtx.AcquireTexture(glImage, 0);
            }
            else
                glImage = ((SwapchainImageOpenGLKHR*)image)->Image; 

            _host.SetRenderTarget(glImage, (uint)data.Size.Width, (uint)data.Size.Height, data.Eye);
            _host.Draw(EngineApp.Current.RenderContext);

            if (useAngle)
            {
               _vulkanCtx!.ReleaseTexture(glImage);
            }


            OpenGLRender.Current.PopGroup();

            return true;
        }

        public XrTextureQuadLayer[]? Layers => _layers;

    }
}
