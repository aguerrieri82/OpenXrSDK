#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Text;

namespace OpenXr.Engine.OpenGL.Oculus
{
    public class GlMultiViewProgram : GlSimpleProgram
    {
        readonly Func<SceneMatrices> GetMatrices;


        private readonly GlBuffer<SceneMatrices> _sceneMatrices;

        public GlMultiViewProgram(GL gl, Func<SceneMatrices> getMatrices, string vSource, string fSource, Func<string, string> includeResolver, GlRenderOptions options)
            : base(gl, vSource, fSource, includeResolver, options)
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

            //var viewId = Locate("VIEW_ID", true);

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
