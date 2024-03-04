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
        public Matrix4x4 ViewProj1;
        public Matrix4x4 ViewProj2;
    }

    public class GlMultiViewRenderTarget : GlTextureRenderTarget, IMultiViewTarget, IShaderHandler
    {
        static SceneMatrices _matrices;

        protected GlMultiViewRenderTarget(GL gl, uint textId, uint sampleCount)
            : base(gl, textId, sampleCount)
        {
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
    }
}
