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
        readonly string? _gSource;

        public GlSimpleProgram(GL gl, string vSource, string fSource, Func<string, string> resolver)
            : base(gl, resolver)
        {
            _fSource = fSource;
            _vSource = vSource;
        }

        public GlSimpleProgram(GL gl, string vSource, string fSource, string? gSource, Func<string, string> resolver)
            : this(gl, vSource, fSource, resolver)
        {
            _gSource = gSource;
        }


        [MemberNotNull(nameof(Vertex))]
        [MemberNotNull(nameof(Fragment))]
        public override void Build()
        {
            Log.Debug(this, "Building program {0}...", _handle);

            var vSource = PatchShader(_vSource, ShaderType.VertexShader);
            var fSource = PatchShader(_fSource, ShaderType.FragmentShader);
            var gSource = _gSource != null ? PatchShader(_gSource, ShaderType.GeometryShader) : null;

            //vSource = ShaderPreprocessor.ParseShader(vSource);

            Vertex = GlShader.GetOrCreate(_gl, ShaderType.VertexShader, vSource);
            Fragment = GlShader.GetOrCreate(_gl, ShaderType.FragmentShader, fSource);

            if (gSource != null)
                Geometry = GlShader.GetOrCreate(_gl, ShaderType.GeometryShader, gSource);

            Create(Vertex, Fragment, Geometry?.Handle ?? 0);

            _values.Clear();
            _locations.Clear();
            _boundBuffers.Clear();

            Log.Debug(this, "Program built");
        }

        public override void Dispose()
        {
            Vertex?.Dispose();
            Fragment?.Dispose();
            Geometry?.Dispose();

            Vertex = null;
            Fragment = null;
            Geometry = null;

            base.Dispose();
        }

        public GlShader? Vertex { get; set; }

        public GlShader? Fragment { get; set; }

        public GlShader? Geometry { get; set; }
    }
}
