#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using XrMath;


namespace XrEngine.OpenGL
{
    public class GlOutlinePass : GlBaseSingleMaterialPass
    {
        protected GlTextureRenderTarget _renderTarget;
        protected int _bindEye;
        protected Size2I _lastSize;
        protected readonly GlTexture _colorTexture;
        protected readonly GlTexture _outlineTexture;
        protected readonly GlComputeProgram _outlineProgram;

        public GlOutlinePass(OpenGLRender renderer, int bindEye)
            : base(renderer)
        {
            _renderTarget = new GlTextureRenderTarget(_gl);
            _bindEye = bindEye;

            _outlineProgram = new GlComputeProgram(renderer.GL, "Image/outline.glsl", str => Embedded.GetString<Material>(str));
            _outlineProgram.Build();

            _colorTexture = new GlTexture(_gl)
            {
                MinFilter = TextureMinFilter.Nearest,
                MagFilter = TextureMagFilter.Nearest,
                MaxLevel = 0,
                IsMutable = true,
                Target = TextureTarget.Texture2D
            };

            _outlineTexture = new GlTexture(_gl)
            {
                MinFilter = TextureMinFilter.Linear,
                MagFilter = TextureMagFilter.Linear,
                MaxLevel = 0,
                IsMutable = true,
                Target = TextureTarget.Texture2D
            };

            _colorTexture.Update(1, new TextureData
            {
                Width = 16,
                Height = 16,
                Format = TextureFormat.Rgba32,
            });

            _renderTarget.FrameBuffer.Configure(_colorTexture, null, 1);
        }

        protected override IGlRenderTarget? GetRenderTarget()
        {
            return _renderTarget;
        }

        protected override bool BeginRender(Camera camera)
        {
            if (Source == null)
            {
                if (!Context.TryRequire<IOutlineSource>(out var source))
                    return false;
                Source = source;
            }

            if (!Source.HasOutlines())
                return false;   

            if (!Equals(camera.ViewSize, _lastSize))
            {
                _lastSize = camera.ViewSize;

                _colorTexture.Update(1, new TextureData
                {
                    Width = _lastSize.Width,
                    Height = _lastSize.Height,
                    Format = TextureFormat.Rgba32,
                });

                _outlineTexture.Update(1, new TextureData
                {
                    Width = _lastSize.Width,
                    Height = _lastSize.Height,
                    Format = TextureFormat.Rgba32,
                });
            }

            _renderTarget.Begin(camera, _lastSize);

            _renderer.State.SetView(_renderer.RenderView);
            _renderer.State.SetClearColor(Color.Transparent);
            _renderer.State.SetWriteDepth(false);
            _renderer.State.SetWriteColor(true);

            _gl.Clear(ClearBufferMask.ColorBufferBit);

            return base.BeginRender(camera);
        }

        protected override bool CanDraw(DrawContent draw)
        {
            if (!Source!.HasOutline(draw.Object!, out var color))
                return false;
            
            _programInstance!.Material.UpdateColor(color);

            return true;
        }

        protected override void EndRender()
        {
            _renderTarget.End(true);

            _outlineProgram.Use();
            _outlineProgram.SetUniform("uSize", (int)_renderer.Options.Outline.Size);

            ProcessImage(_colorTexture, _outlineTexture);

            _renderer.RenderTarget!.Begin(_renderer.UpdateContext.Camera!, _lastSize);

            OverlayTexture(_outlineTexture);
        }

        protected override IEnumerable<GlLayer> SelectLayers()
        {
            return _renderer.Layers
                .Where(a =>
                (a.SceneLayer is DetachedLayer det) &&
                (det.Usage & DetachedLayerUsage.Outline) != 0);
        }

        protected override ShaderMaterial CreateMaterial()
        {
            return new ColorMaterial() 
            { 
                Color = Color.White,
                WriteDepth = false,
                UseDepth = false,   
            };
        }

        public override void Dispose()
        {
            _outlineProgram.Dispose();
            _outlineTexture.Dispose();
            _colorTexture.Dispose();
            _renderTarget.Dispose();
            base.Dispose();
        }

        public IOutlineSource? Source { get; set; }

    }
}
