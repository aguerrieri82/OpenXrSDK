#if GLES
using OpenXr.Framework;
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine.OpenGL.Oculus
{
    public class GlMultiViewProgram : GlProgram
    {
        readonly Func<XrCameraTransform[]> GetTransforms;

        public GlMultiViewProgram(GL gl, Func<XrCameraTransform[]> getTransforms, string vSource, string fSource, GlRenderOptions options)
            : base(gl, vSource, fSource, options)
        {
            GetTransforms = getTransforms; 
        }

        protected override void Create(params uint[] shaders)
        {
            base.Create(shaders);
        }

        public override void SetCamera(Camera camera)
        {
            base.SetCamera(camera);
        }
    }
}
