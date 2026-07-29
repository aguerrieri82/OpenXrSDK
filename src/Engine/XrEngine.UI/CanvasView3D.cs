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


    [UpdateMode(IsParallel = false)]
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
        protected Texture2D? _defLeftTexture;
        protected Texture2D? _defRightTexture;
        protected Texture2D? _lastDrawTexture;
        protected CanvasViewMode _mode;
        protected bool _isStereo;

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

            if (_sizeDirty)
                UpdateSize();

            if (_activeTexture != null && _mode == CanvasViewMode.Texture)
                Draw(ctx);

        }

        public void Draw(RenderContext? ctx)
        {
            var drawTexture = EnableDepthCull && _mode == CanvasViewMode.RenderTarget ?
                _defLeftTexture : _activeTexture;

            if ((NeedDraw || _lastDrawTexture == null) && (_activeEye <= 0 || IsStereo))
            {
                Draw(drawTexture, ctx, Math.Max(_activeEye, 0));

                if (IsStereo && _mode == CanvasViewMode.Texture)
                {
                    Debug.Assert(_activeEye <= 0);
                    Draw(_defRightTexture, ctx, 1);
                }

                _lastDrawTexture = drawTexture;
            }

            else if (_lastDrawTexture != null && _lastDrawTexture != _activeTexture && !EnableDepthCull)
                _scene!.App!.Renderer.CopyTexture(_lastDrawTexture, _activeTexture!);

            if (EnableDepthCull && Mode == CanvasViewMode.RenderTarget)
            {
                if (Context.TryRequire<IQuodDepthCull>(out var depthCull))
                    depthCull.Cull(this);
            }
        }

        protected void Draw(Texture2D? texture, RenderContext? ctx, int activeEye)
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

            Draw(canvas, ctx, activeEye);

            canvas.Flush();

            surface.Flush();

            surfaceProvider.EndDrawSurface(canvas.Surface!, texture);
        }

        protected virtual void Draw(SKCanvas canvas, RenderContext? ctx, int activeEye)
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

            if (_defLeftTexture != null)
            {
                _defLeftTexture.Width = _pixelSize.Width;
                _defLeftTexture.Height = _pixelSize.Height;
                _defLeftTexture.NotifyChanged(ChangeType.Unspecified);

                CreateSurface(_defLeftTexture);
            }

            if (_defRightTexture != null)
            {
                _defRightTexture.Width = _pixelSize.Width;
                _defRightTexture.Height = _pixelSize.Height;
                _defRightTexture.NotifyChanged(ChangeType.Unspecified);

                CreateSurface(_defRightTexture);
            }

            _sizeDirty = false;
        }

        protected virtual Material CreateMaterial(Texture2D leftMain, Texture2D? right)
        {
            if (IsStereo && right != null)
            {
                return new EyeTextureMaterial(leftMain, right)
                {
                    DoubleSided = true,
                    Alpha = AlphaMode.Blend,
                };
            }

            return new TextureMaterial(leftMain)
            {
                DoubleSided = true,
                Alpha = AlphaMode.Blend,
            };
        }

        protected virtual void UpdateMode()
        {
            while (Materials.Count > 0)
                Materials.RemoveAt(0);

            if (_mode == CanvasViewMode.Texture)
            {
                _defLeftTexture ??= new Texture2D
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
                    _defLeftTexture.MipLevelCount = 4;
                    _defLeftTexture.MinFilter = ScaleFilter.LinearMipmapLinear;
                }

                if (IsStereo && Mode == CanvasViewMode.Texture)
                {
                    _defRightTexture ??= new Texture2D
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
                        _defRightTexture.MipLevelCount = 4;
                        _defRightTexture.MinFilter = ScaleFilter.LinearMipmapLinear;
                    }
                }

                Materials.Add(CreateMaterial(_defLeftTexture, _defRightTexture));

                _activeTexture = _defLeftTexture;
            }
            else
                _activeTexture = null;
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

        public bool IsStereo
        {
            get => _isStereo;
            set
            {
                _isStereo = value;
                UpdateMode();
            }
        }

        public Texture2D? DrawTexture => _defLeftTexture;

        public int ActiveEye => _activeEye;

        public float DepthBias { get; set; }

        [Obsolete]
        public bool EnableDepthCull { get; set; }

        public bool UseMips { get; set; }

        public abstract bool NeedDraw { get; }

        public Size2I PixelSize
        {
            get
            {
                if (_sizeDirty)
                    UpdateSize();
                return _pixelSize;
            }
        }

        public Texture2D? ActiveTexture => _activeTexture;

    }
}
