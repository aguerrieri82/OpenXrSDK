#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using OpenXr.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine.OpenGL.Oculus
{

    public class GlMultiViewRenderTarget : GlTextureRenderTarget, IGlProgramFactory, IMultiViewTarget
    {
        XrCameraTransform[]? _transforms;

        protected GlMultiViewRenderTarget(GL gl, uint textId, uint sampleCount)
            : base(gl, textId, sampleCount)
        {
        }

        protected override GlFrameBuffer CreateFrameBuffer(uint texId, uint sampleCount)
        {
            return new GlMultiViewFrameBuffer(_gl, texId, sampleCount);
        }

        public static GlMultiViewRenderTarget Attach(GL gl, uint texId, uint sampleCount)
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
            _transforms = eyes;
        }

        public GlProgram CreateProgram(GL gl, string vSource, string fSource, GlRenderOptions options)
        {
           return new GlMultiViewProgram(gl, ()=> _transforms!, vSource, fSource, options);    
        }
    }
}
