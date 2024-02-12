#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using OpenXr.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine.OpenGL.Oculus
{
    public class GlMultiViewProgram : GlProgram
    {
        readonly Func<SceneMatrices> GetMatrices;


        private readonly GlBuffer<SceneMatrices> _sceneMatrices;

        public GlMultiViewProgram(GL gl, Func<SceneMatrices> getMatrices, string vSource, string fSource, GlRenderOptions options)
            : base(gl, vSource, fSource, options)
        {
            GetMatrices = getMatrices;
            _sceneMatrices = new GlBuffer<SceneMatrices>(_gl, BufferTargetARB.UniformBuffer);
        }

        protected override void PatchShader(string source, ShaderType shaderType, StringBuilder builder)
        {
            if (shaderType == ShaderType.VertexShader)
            {
                builder
                .Append("#define MULTI_VIEW;\n\n");
            }
        }

        public unsafe override void SetCamera(Camera camera)
        {
            var matrices = GetMatrices();

            var buffer = new Span<SceneMatrices>(ref matrices);

            _sceneMatrices.Update(buffer);

            SetUniformBuffer("SceneMatrices", _sceneMatrices);
        }

        public override void Dispose()
        {
            _sceneMatrices.Dispose();
            base.Dispose();
        }
    }
}
