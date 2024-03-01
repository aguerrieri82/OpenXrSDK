#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Security.Cryptography;
using System.Text;
using System.Diagnostics.CodeAnalysis;


namespace Xr.Engine.OpenGL
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
            _programId = Convert.ToBase64String(MD5.HashData(Encoding.UTF8.GetBytes(vSource + fSource)));
        }

        [MemberNotNull(nameof(Vertex))]
        [MemberNotNull(nameof(Fragment))]
        protected override void Build()
        {
            var vSource = PatchShader(_vSource, ShaderType.VertexShader);
            var fSource = PatchShader(_fSource, ShaderType.VertexShader);

            vSource = ShaderPreprocessor.ParseShader(vSource);

            Vertex = new GlShader(_gl, ShaderType.VertexShader, vSource);
            Fragment = new GlShader(_gl, ShaderType.FragmentShader, fSource);

            Create(Vertex, Fragment);
        }

        public GlShader? Vertex { get; set; }

        public GlShader? Fragment { get; set; }
    }
}
