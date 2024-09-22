#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;
using OpenXr.Framework;



namespace XrEngine.OpenGL.Oculus
{

    public struct SceneMatrices
    {
        public Matrix4x4 ViewProj1;

        public Matrix4x4 ViewProj2;

        public Vector3 Position1;

        public float pad1;

        public Vector3 Position2;

        public float pad2;

        public float FarPlane;
    }

    public class GlMultiViewRenderTarget : IGlRenderTarget, IMultiViewTarget, IShaderHandler
    {

        protected GlMultiViewFrameBuffer _frameBuffer;
        protected SceneMatrices _matrices = new();
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

        public void End()
        {
            _frameBuffer.Unbind();
        }

        public uint QueryTexture(FramebufferAttachment attachment)
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

        public bool NeedUpdateShader(UpdateShaderContext ctx, ShaderUpdate lastUpdate)
        {
            return lastUpdate.ShaderHandlers == null || !lastUpdate.ShaderHandlers.Contains(this);
        }

        public GlMultiViewFrameBuffer FrameBuffer => _frameBuffer;

    }
}
