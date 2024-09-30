#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics.CodeAnalysis;


namespace XrEngine.OpenGL
{
    public partial class GlComputeProgram : GlBaseProgram
    {
        readonly string _cSource;


        public GlComputeProgram(GL gl, string cSource,Func<string, string> resolver)
            : base(gl, resolver)
        {
            _cSource = cSource;

        }


        [MemberNotNull(nameof(Computer))]
        public override void Build()
        {
            Log.Debug(this, "Building program {0}...", _handle);

            var cSource = PatchShader(_cSource, ShaderType.ComputeShader);

            Computer = GlShader.GetOrCreate(_gl, ShaderType.ComputeShader, cSource);
            
            Create(Computer);

            _values.Clear();
            _locations.Clear();
            _boundBuffers.Clear();

            Log.Debug(this, "Program built");
        }

        public override void Dispose()
        {
            Computer?.Dispose();
            Computer = null;
            base.Dispose();
        }

        public GlShader? Computer { get; set; }

    }
}
