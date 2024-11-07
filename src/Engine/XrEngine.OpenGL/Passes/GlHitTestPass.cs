#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using XrMath;


namespace XrEngine.OpenGL
{
    public class GlHitTestPass : GlBaseSingleMaterialPass, IViewHitTest
    {
        private readonly GlTextureRenderTarget _renderTarget;
        private readonly GlTexture _colorTexture;
        private readonly GlRenderBuffer _depthBuffer;
        private Size2I _lastSize;
        private readonly List<Object3D?> _objects = [];
        private bool _isBufferValid;
        private readonly IMemoryBuffer<uint> _buffer;

        public GlHitTestPass(OpenGLRender renderer)
            : base(renderer)
        {
            _renderTarget = new GlTextureRenderTarget(_renderer.GL);

            _colorTexture = new GlTexture(_renderer.GL)
            {
                MinFilter = TextureMinFilter.Nearest,
                MagFilter = TextureMagFilter.Nearest,
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

     
            _depthBuffer = new GlRenderBuffer(_renderer.GL);
            _depthBuffer.Update(16, 16, 1, InternalFormat.DepthComponent24);

            _renderTarget.FrameBuffer.Configure(_colorTexture, _depthBuffer, 1);

            _buffer = MemoryBuffer.Create<uint>(1);   
        }

        public unsafe Object3D? HitTest(uint x, uint y)
        {
#if GLES
            throw new NotSupportedException(); 
#else
            if (x >= _lastSize.Width || y >= _lastSize.Height)
                return null;    

            using var data = _buffer.MemoryLock();
            
            if (!_isBufferValid)
            {
                _colorTexture.Bind();
                _renderer.GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                _colorTexture.Unbind();
                _isBufferValid = true;
            }

            y = _lastSize.Height - y;   

            var color = data.Data[y * _lastSize.Width + x];

            if (color < 0 || color >= _objects.Count)
                return null;

            var obj = _objects[(int)color];

            return obj;

            #endif
        }

        protected override IGlRenderTarget? GetRenderTarget()
        {
            return _renderTarget;
        }

        static Color UIntToRGBA(uint color)
        {
            float a = ((color >> 24) & 0xFF) / 255f; 
            float b = ((color >> 16) & 0xFF) / 255f; 
            float g = ((color >> 8) & 0xFF) / 255f;  
            float r = (color & 0xFF) / 255f;         
            return new Color(r, g, b, a);   
        }

        protected override bool PrepareMaterial(Material material)
        {
            var objId = (uint)_objects.Count;

            var mat = (ColorMaterial)_programInstance!.Material;
            mat.WriteDepth = material.WriteDepth;
            mat.UseDepth = material.UseDepth;
            mat.DoubleSided = material.DoubleSided;
            var color = UIntToRGBA(objId);  
            mat.UpdateColor(color);
            return true;
        }

        protected override void Draw(DrawContent draw)
        {
            _objects.Add(draw.Object);  
            draw.Draw!();
        }

        protected override bool BeginRender(Camera camera)
        {
            if (_renderer.RenderTarget is not GlDefaultRenderTarget)
                return false;

            if (!Equals( camera.ViewSize, _lastSize))
            {
                _lastSize = camera.ViewSize;
                _colorTexture.Update(1, new TextureData
                {
                    Width = _lastSize.Width,
                    Height = _lastSize.Height,
                    Format = TextureFormat.Rgba32,
                });
                _depthBuffer.Update(_lastSize.Width, _lastSize.Height, 1, InternalFormat.DepthComponent24);
                _buffer.Allocate(_depthBuffer.Width * _depthBuffer.Height);     
            }

            _renderTarget.Begin(camera, _lastSize);

            _renderer.State.SetView(_renderer.RenderView);
            _renderer.State.SetClearColor(Color.Transparent);
            _renderer.State.SetWriteDepth(true);
            _renderer.State.SetWriteColor(true);
           
            _renderer.GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            _objects.Clear();
            _objects.Add(null);
            _isBufferValid = false;

            return base.BeginRender(camera);
        }

        protected override void EndRender()
        {
            _renderTarget.End(true);
        }

        protected override ShaderMaterial CreateMaterial()
        {
            return new ColorMaterial();
        }

        protected override IEnumerable<GlLayer> SelectLayers()
        {
            return _renderer.Layers.Where(a => a.Type == GlLayerType.Main || a.Type == GlLayerType.Blend);
        }
    }
}
