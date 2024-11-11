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
        protected readonly GlRenderPassTarget _passTarget;
        protected readonly List<Object3D?> _objects = [];

        protected bool _isBufferValid;
        protected Matrix4x4 _lastViewProjInv;
        protected Size2I _lastSize;

        public GlHitTestPass(OpenGLRender renderer)
            : base(renderer)
        {
            _passTarget = new GlRenderPassTarget(renderer.GL);
            _passTarget.DepthFormat = TextureFormat.Depth32Float;

            _passTarget.AddExtra(TextureFormat.RgbFloat32, FramebufferAttachment.ColorAttachment1, true);
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

            _passTarget.FrameBuffer!.Bind();

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
            return _passTarget.RenderTarget;
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

            _passTarget.Configure(camera.ViewSize.Width, camera.ViewSize.Height, TextureFormat.Rgba32);
            
            _lastSize = camera.ViewSize;

            _passTarget.RenderTarget.Begin(camera, _lastSize);

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
            _passTarget.RenderTarget!.End(false);
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
