using CanvasUI;
using SkiaSharp;
using System.Diagnostics;
using System.Numerics;
using XrMath;

namespace XrEngine.UI
{
    public enum CanvasViewMode
    {
        Texture,
        RenderTarget
    }

    public abstract class CanvasView3D : TriangleMesh, IQuodTexture
    {
        static readonly DynamicProp SurfaceProp = new("Surface");

        protected Size2 _size;
        protected Size2I _pixelSize;
        protected float _dpi;
        protected float _dpiScale;
        protected bool _sizeDirty;
        protected Dictionary<uint, Texture2D> _targets = [];
        protected Texture2D? _activeTexture;
        protected int _activeEye;
        protected Texture2D? _defaultTexture;
        protected Texture2D? _lastDrawTexture;
        protected CanvasViewMode _mode;

        public CanvasView3D()
        {
            _dpi = 72;
            _dpiScale = 1;
            _size = new Size2(1, 1);
            _sizeDirty = true;

            var quad = new Quad3D();
            quad.FlipYUV();

            Geometry = quad;
            Mode = CanvasViewMode.Texture;
            UseMips = false;
        }

        public void SetRenderTarget(uint imageId, uint width, uint height, int activeEye = 0)
        {
            if (!_targets.TryGetValue(imageId, out var texture))
            {
                texture = _scene!.App!.Renderer.AttachTexture(imageId);

                if (!EnableDepthCull)
                    CreateSurface(texture);
                
                _targets[imageId] = texture;
            }
            _activeTexture = texture;
            _activeEye = activeEye;
        }


        public override void Update(RenderContext ctx)
        {
            base.Update(ctx);

            if (_activeTexture != null && _mode == CanvasViewMode.Texture)
            {
                if (_sizeDirty)
                    UpdateSize();

                Draw();
            }
        }

        public void Draw()
        {
            var drawTexture = EnableDepthCull && _mode == CanvasViewMode.RenderTarget ? _defaultTexture : _activeTexture;

            if ((NeedDraw || _lastDrawTexture == null) && _activeEye <= 0)
            {
                Draw(drawTexture);
                _lastDrawTexture = drawTexture;
            }

            else if (_lastDrawTexture != null && _lastDrawTexture != _activeTexture && !EnableDepthCull)
                _scene!.App!.Renderer.CopyTexture(_lastDrawTexture, _activeTexture!);

            if (EnableDepthCull)
            {
                if (Context.TryRequire<IQuodDepthCull>(out var depthCull))
                    depthCull.Cull(this);
            }
        }

        protected void Draw(Texture2D? texture)
        {
            Debug.Assert(texture != null);

            var surface = GetSurface(texture);

            if (surface == null)
                return;

            var scaleX = (texture.Width / _pixelSize.Width) * _dpiScale;
            var scaleY = (texture.Height / _pixelSize.Height) * _dpiScale;

            var canvas = surface!.Canvas;

            canvas.SetMatrix(SKMatrix.CreateScale(scaleX, scaleY));

            var surfaceProvider = _scene?.App?.Renderer as ISurfaceProvider;

            surfaceProvider!.BeginDrawSurface(canvas.Surface!, texture);

            Draw(canvas);

            canvas.Flush();

            surface.Flush();

            surfaceProvider.EndDrawSurface(canvas.Surface!, texture);
        }

        protected virtual void Draw(SKCanvas canvas)
        {

        }

        protected SKSurface CreateSurface(Texture2D texture)
        {
            var surface = GetSurface(texture);
            surface?.Dispose();

            var surfaceProvider = _scene?.App?.Renderer as ISurfaceProvider;

            if (surfaceProvider == null)
                throw new NotSupportedException();

            surface = surfaceProvider.CreateSurface(texture);
      
            texture.SetProp(SurfaceProp, surface);

            return surface;
        }

        protected static SKSurface? GetSurface(Texture2D texture)
        {
            return texture.GetProp<SKSurface>(SurfaceProp);
        }

        protected virtual void UpdateSize()
        {
            _pixelSize.Width = (uint)(Size.Width * _dpi / UnitConv.InchesToMeter);
            _pixelSize.Height = (uint)(Size.Height * _dpi / UnitConv.InchesToMeter);

            Transform.Scale = new Vector3(Size.Width, Size.Height, 0.01f);

            if (_defaultTexture != null)
            {
                _defaultTexture.Width = _pixelSize.Width;
                _defaultTexture.Height = _pixelSize.Height;
                _defaultTexture.NotifyChanged(ObjectChangeType.Unspecified);

                CreateSurface(_defaultTexture);
            }

            _sizeDirty = false;
        }

        protected virtual void UpdateMode()
        {
            while (Materials.Count > 0)
                Materials.RemoveAt(0);

            if (_mode == CanvasViewMode.Texture)
            {
                _defaultTexture ??= new Texture2D
                {
                    Format = TextureFormat.Rgba32,
                    WrapS = WrapMode.ClampToEdge,
                    WrapT = WrapMode.ClampToEdge,
                    MinFilter = ScaleFilter.Linear,
                    MagFilter = ScaleFilter.Linear,
                    MipLevelCount = 0
                };

                if (UseMips)
                {
                    _defaultTexture.MipLevelCount = 4;
                    _defaultTexture.MinFilter = ScaleFilter.LinearMipmapLinear;
                }

                Materials.Add(new TextureMaterial(_defaultTexture)
                {
                    DoubleSided = true,
                    Alpha = AlphaMode.Blend,
                });

                _activeTexture = _defaultTexture;
            }
            else
            {
                _activeTexture = null;
                UpdateSize();
                /*
                Materials.Add(new ColorMaterial
                {
                    Color = Color.Transparent,
                    WriteColor = true,
                    WriteDepth = true,
                    DoubleSided = false,
                });

                if (_defaultTexture != null)
                {
                    _defaultTexture.Dispose();
                    _defaultTexture = null;
                }

                _activeTexture = null;

                UpdateSize();
                */
            }
        }


        public CanvasViewMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                UpdateMode();
            }
        }

        public Size2 Size
        {
            get => _size;
            set
            {
                if (_size.Width == value.Width && _size.Height == value.Height)
                    return;
                _size = value;
                _sizeDirty = true;
            }
        }

        public float Dpi
        {
            get => _dpi;
            set
            {
                if (_dpi == value)
                    return;
                _dpi = value;
                _sizeDirty = true;
            }
        }

        public float DpiScale
        {
            get => _dpiScale;
            set
            {
                _dpiScale = value;
            }
        }


        public Texture2D? DrawTexture => _defaultTexture;

        public int ActiveEye => _activeEye;

        public float DepthBias { get; set; }

        public bool EnableDepthCull { get; set; }

        public bool UseMips { get; set; }

        public abstract bool NeedDraw { get; }

        public Size2I PixelSize => _pixelSize;

        public Texture2D? ActiveTexture => _activeTexture;
    }
}
