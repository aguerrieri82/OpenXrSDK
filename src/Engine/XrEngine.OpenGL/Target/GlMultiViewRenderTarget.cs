#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;
using System.Runtime.InteropServices;
using XrMath;


namespace XrEngine.OpenGL
{

    [StructLayout(LayoutKind.Explicit, Size = 176)]
    public struct SceneMatrices
    {
        [FieldOffset(0)]
        public Matrix4x4 ViewProj1;

        [FieldOffset(64)]
        public Matrix4x4 ViewProj2;

        [FieldOffset(128)]
        public Vector3 Position1;

        [FieldOffset(144)]
        public Vector3 Position2;

        [FieldOffset(160)]
        public float FarPlane;
    }

    public class GlMultiViewRenderTarget : IGlRenderTarget, IShaderHandler, IGlFrameBufferProvider
    {
        static readonly InvalidateFramebufferAttachment[] DepthStencilAttachment = [InvalidateFramebufferAttachment.DepthStencilAttachment];

        protected GlMultiViewFrameBuffer _frameBuffer;
        protected static SceneMatrices _matrices = new();
        readonly GL _gl;


        public GlMultiViewRenderTarget(GL gl)
        {
            _frameBuffer = new GlMultiViewFrameBuffer(gl);
            _gl = gl;
        }

        public void Begin(Camera camera)
        {
            camera.ViewSize = _frameBuffer.Size;
            GlState.Current!.SetView(new Rect2I(camera.ViewSize));

            _frameBuffer.Bind();

            var eyes = camera.Eyes;

            if (eyes == null)
                return;

            _matrices.ViewProj1 = eyes[0].ViewProj;
            _matrices.ViewProj2 = eyes[1].ViewProj;
            _matrices.Position1 = eyes[0].World.Translation;
            _matrices.Position2 = eyes[1].World.Translation;
            _matrices.FarPlane = camera.Far;
        }

        public void End(bool discardDepth)
        {
            if (discardDepth)
            {
                _frameBuffer.Bind();
                _gl.InvalidateFramebuffer(_frameBuffer.Target, DepthStencilAttachment);
            }

            _frameBuffer.Unbind();
        }

        public GlTexture? QueryTexture(FramebufferAttachment attachment)
        {
            return _frameBuffer.QueryTexture(attachment);
        }

        public void Dispose()
        {
            _frameBuffer.Dispose();
            GC.SuppressFinalize(this);
        }

        public void UpdateShader(ShaderUpdateBuilder bld)
        {
            bld.AddExtension("GL_OVR_multiview2");

            bld.AddFeature("MULTI_VIEW");

            bld.LoadBuffer(ctx => (SceneMatrices?)_matrices, 10, BufferStore.Shader);
        }

        public bool NeedUpdateShader(UpdateShaderContext ctx)
        {
            return ctx.LastGlobalUpdate?.ShaderHandlers == null || !ctx.LastGlobalUpdate.ShaderHandlers.Contains(this);
        }

        public void CommitDepth()
        {
            _gl.Flush();
        }

        public GlMultiViewFrameBuffer FrameBuffer => _frameBuffer;

        IGlFrameBuffer IGlFrameBufferProvider.FrameBuffer => _frameBuffer;
    }
}
