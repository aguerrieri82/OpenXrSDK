#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics.CodeAnalysis;


namespace XrEngine.OpenGL
{
    public partial class GlSimpleProgram : GlBaseProgram
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
            Log.Debug(this, "Building program {0}...", _handle);

            var vSource = PatchShader(_vSource, ShaderType.VertexShader);
            var fSource = PatchShader(_fSource, ShaderType.FragmentShader);

            //vSource = ShaderPreprocessor.ParseShader(vSource);

            Vertex = GlShader.GetOrCreate(_gl, ShaderType.VertexShader, vSource);
            Fragment = GlShader.GetOrCreate(_gl, ShaderType.FragmentShader, fSource);

            Create(Vertex, Fragment);

            Log.Debug(this, "Program built");
        }

        public override void Dispose()
        {
            Vertex?.Dispose();
            Fragment?.Dispose();

            Vertex = null;
            Fragment = null;

            base.Dispose();
        }

        public GlShader? Vertex { get; set; }

        public GlShader? Fragment { get; set; }
    }
}
