#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;
using OpenXr.Framework;

namespace Xr.Engine.OpenGL.Oculus
{

    public struct SceneMatrices
    {
        public Matrix4x4 View1;
        public Matrix4x4 View2;
        public Matrix4x4 Projection1;
        public Matrix4x4 Projection2;

    }

    public class GlMultiViewRenderTarget : GlTextureRenderTarget, IMultiViewTarget, IShaderHandler
    {
        static SceneMatrices _matrices;
        private readonly GlBuffer<SceneMatrices> _sceneMatrices;


        protected GlMultiViewRenderTarget(GL gl, uint textId, uint sampleCount)
            : base(gl, textId, sampleCount)
        {
            _sceneMatrices = new GlBuffer<SceneMatrices>(_gl, BufferTargetARB.UniformBuffer);
        }

        protected override GlFrameBuffer CreateFrameBuffer(uint texId, uint sampleCount)
        {
            return new GlMultiViewFrameBuffer(_gl, texId, sampleCount);
        }

        public static new GlMultiViewRenderTarget Attach(GL gl, uint texId, uint sampleCount)
        {
            if (!_targets.TryGetValue(texId, out var target))
            {
                target = new GlMultiViewRenderTarget(gl, texId, sampleCount);
                _targets[texId] = target;
            }

            return (GlMultiViewRenderTarget)target;
        }

        public void SetCameraTransforms(XrCameraTransform[] eyes)
        {
            _matrices.Projection1 = eyes[0].Projection;
            _matrices.Projection2 = eyes[1].Projection;
            Matrix4x4.Invert(eyes[0].Transform, out _matrices.View1);
            Matrix4x4.Invert(eyes[1].Transform, out _matrices.View2);
        }

        public void UpdateShader(ShaderUpdateBuilder bld)
        {
            bld.AddExtension("GL_OVR_multiview2");

            bld.AddFeature("MULTI_VIEW");

            bld.SetUniform("SceneMatrices", ctx =>
            {
                var buffer = new Span<SceneMatrices>(ref _matrices);

                _sceneMatrices.Update(buffer);

                return (IBuffer)_sceneMatrices;
            });
        }

        public bool NeedUpdateShader(UpdateShaderContext ctx, ShaderUpdate lastUpdate)
        {
            return true;
        }
    }
}
