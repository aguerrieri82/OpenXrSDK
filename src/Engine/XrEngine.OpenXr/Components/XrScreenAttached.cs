using OpenXr.Framework;
using OpenXr.Framework.Angle;
using OpenXr.Framework.Layers;
using Silk.NET.OpenXR;
using System.Diagnostics;
using System.Numerics;
using XrEngine.OpenGL;
using XrMath;

namespace XrEngine.OpenXr
{
    public class XrScreenAttached : Behavior<CurvedScreen>, IDisposable
    {
        readonly IGeometryLayerSource _source;
        readonly Texture2D? _texture;

        readonly Dictionary<Material, bool> _materialStates = [];

        XrApp? _app;
        XrCylinderLayer? _layer;
        TextureMaterial? _textureMaterial;
        DepthOnlyMaterial? _depthMaterial;
        AngleVulkanContext? _vulkanCtx;

        bool _xrMode;


        public unsafe XrScreenAttached(Texture2D texture)
        {
            _texture = texture;

            _textureMaterial = new TextureMaterial(texture)
            {
            };

            _source = new XrTextureLayerSource(
                RenderTexture,
                new Size2I(texture.Width, texture.Height));
        }

        public XrScreenAttached(IGeometryLayerSource source)
        {
            _source = source;
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

        protected override void Update(RenderContext ctx)
        {
            var app = XrApp.Current;

            if (app != _app)
            {
                DetachXr();

                if (app != null)
                    AttachXr(app);
            }

            SetXrMode(_app?.IsStarted == true);
        }

        private void AttachXr(XrApp app)
        {
            _app = app;

            _layer = new XrCylinderLayer(GetCylinder, _source)
            {
                Priority = XrLayerPriority.UiGeomeytry,
                FlipY = false
            };

            _app.Layers.Add(_layer);
        }

        private void DetachXr()
        {
            SetXrMode(false);

            if (_layer != null)
            {
                _app?.Layers.List.Remove(_layer);
                _layer.Dispose();
                _layer = null;
            }

            _app = null;
        }

        private Cylinder GetCylinder()
        {
            Debug.Assert(_host != null);

            var radius = _host.Width / _host.Curvature;
            var orientation = _host.WorldOrientation;

            var center = _host.WorldPosition +
                Vector3.Transform(Vector3.UnitZ * radius, orientation);

            return new Cylinder
            {
                Pose = new Pose3
                {
                    Position = center,
                    Orientation = orientation
                },
                Radius = radius,
                Height = _host.Height,
                Angle = _host.Curvature
            };
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

            _depthMaterial.IsEnabled = true;
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

            _texture!.ToGlTexture().BlitTo(
                GlTexture.Attach(OpenGLRender.Current.GL, glImage), true);
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

        public XrCylinderLayer? Layer => _layer;
    }
}