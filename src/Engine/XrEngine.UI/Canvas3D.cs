using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrMath;
using static System.Net.Mime.MediaTypeNames;

namespace XrEngine.UI
{
    public abstract class Canvas3D : TriangleMesh
    {
        protected Vector2 _size;
        protected Size2I _pixelSize;
        protected float _dpi;
        protected float _dpiScale;
        protected bool _sizeDirty;
        protected Dictionary<nint, Texture2D> _targets = [];
        protected Texture2D? _activeTexture;
        protected Texture2D _defaultTexture;

        public Canvas3D()
        {
            _dpi = 72;
            _size = new Vector2(1, 0.56f);
            _sizeDirty = true;

            _defaultTexture = new Texture2D
            {
                Format = TextureFormat.Rgba32,
                WrapS = WrapMode.ClampToEdge,
                WrapT = WrapMode.ClampToEdge,
                MinFilter = ScaleFilter.LinearMipmapLinear,
                MagFilter = ScaleFilter.Linear, 
            };

            _activeTexture = _defaultTexture;

            //Materials.Add(new ColorMaterial() { DoubleSided = true, Color = Color.White });

            Materials.Add(new TextureMaterial(_defaultTexture) { DoubleSided = true, Color = Color.White });

            Geometry = Cube3D.Instance;
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
            if (_sizeDirty)
                UpdateSize();

            if (_activeTexture != null && NeedDraw)
            {
                var surface = GetSurface(_activeTexture);

                var scaleX = (_activeTexture.Width / _pixelSize.Width) * _dpiScale;
                var scaleY = (_activeTexture.Height / _pixelSize.Height) * _dpiScale;

                var canvas = surface!.Canvas;

                canvas.SetMatrix(SKMatrix.CreateScale(scaleX, scaleY));    

                Draw(canvas);

                canvas.Flush();

                surface.Flush(true);
            }

            base.Update(ctx);
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
    
        protected SKSurface? GetSurface(Texture2D texture)
        {
            return texture.GetProp<SKSurface>("Surface");
        }

        protected virtual void UpdateSize()
        {
            _pixelSize.Width = (uint)(Size.X * _dpi / UnitConv.InchesToMeter);
            _pixelSize.Height = (uint)(Size.Y * _dpi / UnitConv.InchesToMeter);

            Transform.Scale = new Vector3(Size.X, Size.Y, 0.01f);

            if (_activeTexture == _defaultTexture)
            {
                _activeTexture.Width = _pixelSize.Width;
                _activeTexture.Height = _pixelSize.Height;

                CreateSurface(_activeTexture);
            }

            _sizeDirty = false;
        }

        protected abstract bool NeedDraw { get; }

        public Vector2 Size
        {
            get => _size;
            set
            {
                if (_size == value)
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
