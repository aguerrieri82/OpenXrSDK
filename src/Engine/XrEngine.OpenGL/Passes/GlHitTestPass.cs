#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;
using XrMath;


namespace XrEngine.OpenGL
{
    public class GlHitTestPass : GlBaseSingleMaterialPass, IViewHitTest
    {
        protected readonly GlTextureRenderTarget _renderTarget;
        protected readonly GlTexture _colorTexture;
        protected readonly GlTexture _depthTexture;
        protected readonly List<Object3D?> _objects = [];
        protected readonly GlTexture _normalTexture;

        protected Size2I _lastSize;
        protected bool _isBufferValid;
        protected Matrix4x4 _lastViewProjInv;

        public GlHitTestPass(OpenGLRender renderer)
            : base(renderer)
        {
            _renderTarget = new GlTextureRenderTarget(_gl);

            _colorTexture = new GlTexture(_gl)
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

            _depthTexture = new GlTexture(_gl)
            {
                MinFilter = TextureMinFilter.Nearest,
                MagFilter = TextureMagFilter.Nearest,
                MaxLevel = 0,
                IsMutable = true,
                Target = TextureTarget.Texture2D
            };

            _depthTexture.Update(1, new TextureData
            {
                Width = 16,
                Height = 16,
                Format = TextureFormat.Depth32Float,
            });

            _renderTarget.FrameBuffer.Configure(_colorTexture, _depthTexture, 1);
            _normalTexture = _renderTarget.FrameBuffer.GetOrCreateEffect(FramebufferAttachment.ColorAttachment1);
        }

        public unsafe HitTestResult HitTest(uint x, uint y)
        {
            var result = new HitTestResult();

            if (x >= _lastSize.Width || y >= _lastSize.Height)
                return result;    

            uint objId = 0;
            Vector3 normal = Vector3.Zero;
            float depth = 1;
            var txY = _lastSize.Height - y;

            _renderTarget.FrameBuffer.Bind();
            _gl.ReadBuffer(ReadBufferMode.ColorAttachment0);
            _gl.ReadPixels((int)x, (int)txY, 1, 1, PixelFormat.Rgba, PixelType.UnsignedByte, &objId);
            _gl.ReadBuffer(ReadBufferMode.ColorAttachment1);
            _gl.ReadPixels((int)x, (int)txY, 1, 1, PixelFormat.Rgb, PixelType.Float, &normal);
            _gl.ReadPixels((int)x, (int)txY, 1, 1, PixelFormat.DepthComponent, PixelType.Float, &depth);
            
            if (objId <= 0 || objId >= _objects.Count)
                return result;

            result.Object = _objects[(int)objId];
            result.Normal = normal;
            result.Depth = depth;
            result.Pos = ToView(x, y, result.Depth).Project(_lastViewProjInv);

            return result;
        }

        protected override IGlRenderTarget? GetRenderTarget()
        {
            return _renderTarget;
        }

        protected override UpdateProgramResult UpdateProgram(UpdateShaderContext updateContext, Material drawMaterial)
        {
            var objId = (uint)_objects.Count;

            var effect = (HitTestEffect)_programInstance!.Material;

            effect.WriteDepth = drawMaterial.WriteDepth;
            effect.UseDepth = drawMaterial.UseDepth;
            effect.DoubleSided = drawMaterial.DoubleSided;
            effect.DrawId = objId;

            return base.UpdateProgram(updateContext, drawMaterial);
        }


        protected override void Draw(DrawContent draw)
        {
            _objects.Add(draw.Object);  
            draw.Draw!();
        }

        protected Vector3 ToView(uint x, uint y, float z)
        { 
            return new Vector3(
                2.0f * x / _lastSize.Width - 1.0f,
                1.0f - 2.0f * y / _lastSize.Height,
                2f * z - 1f
            );
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
                
                _normalTexture.Update(1, new TextureData
                {
                    Width = _lastSize.Width,
                    Height = _lastSize.Height,
                    Format = TextureFormat.RgbFloat32,
                });

                _depthTexture.Update(1, new TextureData
                {
                    Width = _lastSize.Width,
                    Height = _lastSize.Height,
                    Format = TextureFormat.Depth32Float
                });
            }

            _renderTarget.Begin(camera, _lastSize);

            _renderer.State.SetView(_renderer.RenderView);
            _renderer.State.SetClearColor(Color.Transparent);
            _renderer.State.SetWriteDepth(true);
            _renderer.State.SetWriteColor(true);

            _gl.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            _objects.Clear();
            _objects.Add(null);
            _isBufferValid = false;

            _lastViewProjInv = camera.ViewProjectionInverse;

            return base.BeginRender(camera);
        }

        protected override void EndRender()
        {
            _renderTarget.End(false);
        }

        protected override ShaderMaterial CreateMaterial()
        {
            return new HitTestEffect();
        }

        protected override IEnumerable<GlLayer> SelectLayers()
        {
            return _renderer.Layers.Where(a => a.Type == GlLayerType.Main || a.Type == GlLayerType.Blend);
        }
    }
}
