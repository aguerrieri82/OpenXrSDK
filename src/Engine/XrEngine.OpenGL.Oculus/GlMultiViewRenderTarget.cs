#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;
using OpenXr.Framework;
using System.Runtime.InteropServices;



namespace XrEngine.OpenGL.Oculus
{

    [StructLayout(LayoutKind.Explicit,Size = 176)]     
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

    public class GlMultiViewRenderTarget : IGlRenderTarget, IMultiViewTarget, IShaderHandler, IGlFrameBufferProvider
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

        public void Begin()
        {
            _frameBuffer.Bind();
        }

        public void End(bool finalPass)
        {
            if (finalPass && false)
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
        }

        public void SetCameraTransforms(XrCameraTransform[] eyes, float farPlane)
        {
            var trans1 = new Transform3D();
            var trans2 = new Transform3D();
            trans1.SetMatrix(eyes[0].Transform);
            trans2.SetMatrix(eyes[1].Transform);

            Matrix4x4.Invert(eyes[0].Transform, out var view1);
            Matrix4x4.Invert(eyes[1].Transform, out var view2);

            _matrices.ViewProj1 = view1 * eyes[0].Projection;
            _matrices.ViewProj2 = view2 * eyes[1].Projection;
            _matrices.Position1 = trans1.Position;
            _matrices.Position2 = trans2.Position;
            _matrices.FarPlane = farPlane;
        }

        public void UpdateShader(ShaderUpdateBuilder bld)
        {
            bld.AddExtension("GL_OVR_multiview2");

            bld.AddFeature("MULTI_VIEW");

            bld.SetUniformBuffer("SceneMatrices", ctx => (SceneMatrices?)_matrices, 10, true);
        }

        public bool NeedUpdateShader(UpdateShaderContext ctx)
        {
            return ctx.LastUpdate?.ShaderHandlers == null || !ctx.LastUpdate.ShaderHandlers.Contains(this);
        }

        public void CommitDepth()
        {
            _gl.Flush();
        }

        public GlMultiViewFrameBuffer FrameBuffer => _frameBuffer;

        IGlFrameBuffer IGlFrameBufferProvider.FrameBuffer => _frameBuffer;
    }
}
