#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics.CodeAnalysis;


namespace XrEngine.OpenGL
{
    public partial class GlSimpleProgram : GlProgram
    {
        readonly string _vSource;
        readonly string _fSource;


        public GlSimpleProgram(GL gl, string vSource, string fSource, Func<string, string> resolver)
            : base(gl, resolver)
        {
            _fSource = fSource;
            _vSource = vSource;
        }

        [MemberNotNull(nameof(Vertex))]
        [MemberNotNull(nameof(Fragment))]
        public override void Build()
        {
            var vSource = PatchShader(_vSource, ShaderType.VertexShader);
            var fSource = PatchShader(_fSource, ShaderType.FragmentShader);

            vSource = ShaderPreprocessor.ParseShader(vSource);

            Vertex = new GlShader(_gl, ShaderType.VertexShader, vSource);
            Fragment = new GlShader(_gl, ShaderType.FragmentShader, fSource);

            Create(Vertex, Fragment);
        }

        public GlShader? Vertex { get; set; }

        public GlShader? Fragment { get; set; }
    }
}
