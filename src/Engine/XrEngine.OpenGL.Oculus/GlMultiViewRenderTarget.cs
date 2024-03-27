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
    }

    public class GlMultiViewRenderTarget : IGlRenderTarget, IMultiViewTarget, IShaderHandler
    {
        static GlMultiViewRenderTarget? _instance;
        
        protected GlMultiViewFrameBuffer _frameBuffer;
        protected SceneMatrices _matrices = new SceneMatrices();
        GL _gl;


        protected GlMultiViewRenderTarget(GL gl)
        {
            _frameBuffer = new GlMultiViewFrameBuffer(gl);
            _gl = gl;   

        }

        public static GlMultiViewRenderTarget Attach(GL gl, uint colorTex, uint depthTex, uint sampleCount)
        {
            if (_instance == null)
                _instance = new GlMultiViewRenderTarget(gl);

            _instance.FrameBuffer.Configure(colorTex, depthTex, sampleCount);

            return _instance;
        }

        public void Begin()
        {
            _frameBuffer.BindDraw();
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
     
        public void SetCameraTransforms(XrCameraTransform[] eyes)
        {
            Matrix4x4.Invert(eyes[0].Transform, out var view1);
            Matrix4x4.Invert(eyes[1].Transform, out var view2);

            _matrices.ViewProj1 = view1 * eyes[0].Projection;
            _matrices.ViewProj2 = view2 * eyes[1].Projection;
        }

        public void UpdateShader(ShaderUpdateBuilder bld)
        {
            bld.AddExtension("GL_OVR_multiview2");

            bld.AddFeature("MULTI_VIEW");

            bld.SetUniformBuffer("SceneMatrices", ctx => _matrices, true);
        }

        public bool NeedUpdateShader(UpdateShaderContext ctx, ShaderUpdate lastUpdate)
        {
            return true;
        }

        public GlMultiViewFrameBuffer FrameBuffer => _frameBuffer;

    }
}
