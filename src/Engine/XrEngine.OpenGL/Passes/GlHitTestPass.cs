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
            _passTarget.DepthFormat = TextureFormat.Depth24;

            _passTarget.AddExtra(TextureFormat.RgbFloat16, FramebufferAttachment.ColorAttachment1, true);
        }

        public unsafe HitTestResult HitTest(uint x, uint y)
        {
            var result = new HitTestResult();

            if (x >= _lastSize.Width || y >= _lastSize.Height)
                return result;

            var ids = new uint[2];
            var normal = Vector3.Zero;
            float depth = 1;
            var txY = _lastSize.Height - y;

            _passTarget.FrameBuffer!.BindRead(ReadBufferMode.ColorAttachment0);

            _gl.ReadPixels((int)x, (int)txY, 1, 1, PixelFormat.RGInteger, PixelType.UnsignedInt, ids);

            _passTarget.FrameBuffer!.BindRead(ReadBufferMode.ColorAttachment1);
            _gl.ReadPixels((int)x, (int)txY, 1, 1, PixelFormat.Rgb, PixelType.Float, &normal);
            _gl.ReadPixels((int)x, (int)txY, 1, 1, PixelFormat.DepthComponent, PixelType.Float, &depth);

            if (ids[0] <= 0 || ids[0] >= _objects.Count)
                return result;

            result.Object = _objects[(int)ids[0]];
            result.Normal = normal;
            result.Depth = depth;
            result.Pos = ToView(x, y, result.Depth).Project(_lastViewProjInv);
            result.Index = ids[1];

            return result;
        }

        protected override IGlRenderTarget? GetRenderTarget()
        {
            return _passTarget.RenderTarget;
        }

        protected override UpdateProgramResult UpdateProgram(UpdateShaderContext updateContext, Material drawMaterial)
        {
            var effect = (HitTestEffect)_programInstance!.Material;

            effect.WriteDepth = drawMaterial.WriteDepth;
            effect.UseDepth = drawMaterial.UseDepth;
            effect.DoubleSided = drawMaterial.DoubleSided;

            if (drawMaterial is ShaderMaterial mat)
                effect.HasSkin = mat.HasSkin;

            return base.UpdateProgram(updateContext, drawMaterial);
        }

        protected override UpdateProgramResult UpdateProgram(UpdateShaderContext updateContext, Object3D model)
        {
            var objId = (uint)_objects.Count;
            var effect = (HitTestEffect)_programInstance!.Material;
            effect.DrawId = objId;
            return UpdateProgramResult.Changed;
        }

        protected override bool CanDraw(DrawContent draw)
        {
            if (draw.Object is SplatMesh)
                return false; 
            return base.CanDraw(draw);
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

            _passTarget.Configure(camera.ViewSize.Width, camera.ViewSize.Height, TextureFormat.RgUint32);

            if (_passTarget.RenderTarget == null)
                return false;

            _lastSize = camera.ViewSize;

       
            _passTarget.RenderTarget.Begin(camera);
            _passTarget.FrameBuffer!.BindDraw(DrawBufferMode.ColorAttachment0, DrawBufferMode.ColorAttachment1);

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

        protected override IEnumerable<IGlLayer> SelectLayers()
        {
            return _renderer.Layers.Where(a => a.Type == GlLayerType.Opaque || a.Type == GlLayerType.Blend);
        }
    }
}
