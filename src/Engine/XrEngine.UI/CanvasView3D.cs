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

    public abstract class CanvasView3D : TriangleMesh
    {
        protected Size2 _size;
        protected Size2I _pixelSize;
        protected float _dpi;
        protected float _dpiScale;
        protected bool _sizeDirty;
        protected Dictionary<nint, Texture2D> _targets = [];
        protected Texture2D? _activeTexture;
        protected Texture2D? _defaultTexture;
        protected CanvasViewMode _mode;

        public CanvasView3D()
        {
            _dpi = 72;
            _size = new Size2(1, 0.56f);
            _sizeDirty = true;

            Geometry = Quad3D.Default;

            Mode = CanvasViewMode.Texture;
        }

        public void SetRenderTarget(nint imageId, uint width, uint height)
        {
            if (!_targets.TryGetValue(imageId, out var texture))
            {
                texture = new Texture2D
                {
                    Width = width,
                    Height = height,
                    Format = TextureFormat.Rgba32
                };
                CreateSurface(texture, imageId);
                _targets[imageId] = texture;
            }
            _activeTexture = texture;
        }


        public override void Update(RenderContext ctx)
        {
            if (_activeTexture != null && NeedDraw && _mode == CanvasViewMode.Texture)
            {
                if (_sizeDirty)
                    UpdateSize();

                Draw();
            }

            base.Update(ctx);
        }

        public void Draw()
        {
            Debug.Assert(_activeTexture != null);

            var surface = GetSurface(_activeTexture);

            var scaleX = (_activeTexture.Width / _pixelSize.Width) * _dpiScale;
            var scaleY = (_activeTexture.Height / _pixelSize.Height) * _dpiScale;

            var canvas = surface!.Canvas;

            canvas.SetMatrix(SKMatrix.CreateScale(scaleX, scaleY));

            var surfaceProvider = _scene?.App?.Renderer as ISurfaceProvider;

            surfaceProvider!.BeginDrawSurface();

            Draw(canvas);

            canvas.Flush();

            surface.Flush();

            surfaceProvider.EndDrawSurface();
        }

        protected virtual void Draw(SKCanvas canvas)
        {

        }

        protected SKSurface CreateSurface(Texture2D texture, nint imageId = 0)
        {
            var surface = GetSurface(texture);
            surface?.Dispose();

            var surfaceProvider = _scene?.App?.Renderer as ISurfaceProvider;

            if (surfaceProvider == null)
                throw new NotSupportedException();

            surface = surfaceProvider.CreateSurface(texture, imageId);
            texture.SetProp("Surface", surface);

            return surface;
        }

        protected static SKSurface? GetSurface(Texture2D texture)
        {
            return texture.GetProp<SKSurface>("Surface");
        }

        protected virtual void UpdateSize()
        {
            _pixelSize.Width = (uint)(Size.Width * _dpi / UnitConv.InchesToMeter);
            _pixelSize.Height = (uint)(Size.Height * _dpi / UnitConv.InchesToMeter);

            Transform.Scale = new Vector3(Size.Width, Size.Height, 0.01f);

            if (_defaultTexture != null && _activeTexture == _defaultTexture)
            {
                _activeTexture.Width = _pixelSize.Width;
                _activeTexture.Height = _pixelSize.Height;

                CreateSurface(_activeTexture);
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
                    MinFilter = ScaleFilter.LinearMipmapLinear,
                    MagFilter = ScaleFilter.Linear,
                };

                Materials.Add(new TextureMaterial(_defaultTexture)
                {
                    DoubleSided = true,
                    Alpha = AlphaMode.Blend,
                });

                _activeTexture = _defaultTexture;
            }
            else
            {
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
            }
        }


        public abstract bool NeedDraw { get; }

        public Size2I PixelSize => _pixelSize;

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

    }
}
