using OpenXr.Framework;
using OpenXr.Framework.Angle;
using OpenXr.Framework.Layers;
using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using XrEngine.OpenGL;
using XrMath;

namespace XrEngine.OpenXr.Components
{
    public abstract class BaseXrLayerAttach<TObj, TLayer> : BaseXrComponent<TObj>, IDisposable 
        where TObj : TriangleMesh
        where TLayer : class, IXrLayer
    {
        protected IGeometryLayerSource? _source;
        protected Texture2D? _texture;
        protected Dictionary<Material, bool> _materialStates = [];

        protected TextureMaterial? _textureMaterial;
        protected DepthOnlyMaterial? _depthMaterial;
        protected AngleVulkanContext? _vulkanCtx;
        protected bool _xrMode;
        protected TLayer? _layer;


        protected abstract TLayer CreateLayer();

        protected override void AttachXr()
        {
            _layer = CreateLayer();

            _xrApp!.Layers.Add(_layer);

            SetXrMode(true);
        }

        protected override void DetachXr()
        {
            SetXrMode(false);

            if (_layer != null)
            {
                _xrApp!.Layers.Remove(_layer);
                _layer.Dispose();
                _layer = null;
            }
        }

        protected override void OnAttach()
        {
            Debug.Assert(_host != null);

            if (_textureMaterial != null)
                _host.Materials.Add(_textureMaterial);

            _depthMaterial = new DepthOnlyMaterial
            {
                IsEnabled = false
            };

            _host.Materials.Add(_depthMaterial);
        }


        protected unsafe void Initialize(Texture2D texture)
        {
            _texture = texture;

            _textureMaterial = new TextureMaterial(texture)
            {
            };

            _source = new XrTextureLayerSource(
                RenderTexture,
                new Size2I(texture.Width, texture.Height));

            FlipY = true;
        }

        private void SetXrMode(bool value)
        {
            if (_xrMode == value)
                return;

            _xrMode = value;

            if (_xrMode)
                EnableXrMode();
            else
                DisableXrMode();
        }

        private void EnableXrMode()
        {
            Debug.Assert(_host != null && _depthMaterial != null);

            _materialStates.Clear();

            foreach (var material in _host.Materials)
            {
                if (material == _depthMaterial)
                    continue;

                _materialStates[material] = material.IsEnabled;
                material.IsEnabled = false;
            }

            _depthMaterial.IsEnabled = _layer!.Priority < XrLayerPriority.Projection;
        }

        private void DisableXrMode()
        {
            foreach (var item in _materialStates)
                item.Key.IsEnabled = item.Value;

            _materialStates.Clear();

            if (_depthMaterial != null)
                _depthMaterial.IsEnabled = false;
        }

        private unsafe bool RenderTexture(GeometryRenderData data, SwapchainImageBaseHeader* image, long predTime)
        {
            var swapchain = data.Swapchain!;

            var useAngle = OpenGLRender.Current!.Features.IsAngle;

            uint glImage;

            if (useAngle)
            {
                _vulkanCtx ??= Context.Require<AngleVulkanContext>();

                glImage = _vulkanCtx.AttachVulkanImage(image, swapchain).Texture;
            }
            else
                glImage = ((SwapchainImageOpenGLKHR*)image)->Image;

            if (FlipY && !SupportNativeFlip)
            {
                _texture!.ToGlTexture().BlitTo(
                    GlTexture.Attach(OpenGLRender.Current.GL, glImage), true);
            }
            else
                _texture!.ToGlTexture().CopyTo(GlTexture.Attach(OpenGLRender.Current.GL, glImage));

            return true;
        }


        public void Dispose()
        {
            DetachXr();

            if (_host != null)
            {
                if (_depthMaterial != null)
                    _host.Materials.Remove(_depthMaterial);

                if (_textureMaterial != null)
                    _host.Materials.Remove(_textureMaterial);
            }

            _depthMaterial?.Dispose();
            _depthMaterial = null;

            _textureMaterial?.Dispose();
            _textureMaterial = null;

            GC.SuppressFinalize(this);
        }

        protected bool SupportNativeFlip => !OperatingSystem.IsWindows();

        public bool FlipY { get; set; }

        public TLayer? Layer => _layer;

    }
}
