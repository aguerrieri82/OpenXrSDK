#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using XrEngine.Helpers;

namespace XrEngine.OpenGL
{
    public partial class GlSimpleProgram : GlBaseProgram
    {
        readonly string _vSourceName;
        readonly string _fSourceName;
        readonly string? _gSourceName;
        readonly string? _tcSourceName;
        readonly string? _teSourceName;

        protected bool _isBuilt;

        public GlSimpleProgram(GL gl, byte[] binary, GLEnum format)
             : base(gl, str => throw new NotSupportedException())
        {
            _vSourceName = "";
            _fSourceName = "";

            Load(binary, format);

            _isBuilt = true;
        }

        public GlSimpleProgram(GL gl, string vSource, string fSource, Func<string, string?> resolver)
            : base(gl, resolver)
        {
            _fSourceName = fSource;
            _vSourceName = vSource;
        }

        public GlSimpleProgram(GL gl, string vSource, string fSource, string? gSource, string? tcSource, string? teSource, Func<string, string?> resolver)
            : this(gl, vSource, fSource, resolver)
        {
            _gSourceName = gSource;
            _tcSourceName = tcSource;
            _teSourceName = teSource;
        }

        public override bool Build(string? cachePath = null, Func<ulong, bool>? validateHash = null)
        {
            Log.Info(this, "Building program {0}...", _handle);

            var vSource = PatchShader(_vSourceName, ShaderType.VertexShader);
            var fSource = PatchShader(_fSourceName, ShaderType.FragmentShader);
            var gSource = _gSourceName != null ? PatchShader(_gSourceName, ShaderType.GeometryShader) : null;
            var tcSource = _tcSourceName != null ? PatchShader(_tcSourceName, ShaderType.TessControlShader) : null;
            var teSource = _teSourceName != null ? PatchShader(_teSourceName, ShaderType.TessEvaluationShader) : null;

            UpdateSourceHash();

            if (validateHash != null && !validateHash(_sourceHash))
                return false;

            if (cachePath == null || !TryReadCache(cachePath))
            {
                Vertex = GlShader.GetOrCreate(_gl, ShaderType.VertexShader, vSource, _vSourceName);
                Fragment = GlShader.GetOrCreate(_gl, ShaderType.FragmentShader, fSource, _fSourceName);

                if (gSource != null)
                    Geometry = GlShader.GetOrCreate(_gl, ShaderType.GeometryShader, gSource, _gSourceName);

                if (tcSource != null)
                    TessControl = GlShader.GetOrCreate(_gl, ShaderType.TessControlShader, tcSource, _tcSourceName);

                if (teSource != null)
                    TessEval = GlShader.GetOrCreate(_gl, ShaderType.TessEvaluationShader, teSource, _teSourceName);

                Create(Vertex, Fragment, Geometry?.Handle ?? 0, TessControl?.Handle ?? 0, TessEval?.Handle ?? 0);

                if (cachePath != null)
                    WriteCache(cachePath);
            }

            ClearCache();

            Log.Debug(this, "Program built");

            _isBuilt = true;

            return true;
        }

        protected override void UpdateMeta(ProgramMeta meta)
        {
            meta.VSource = _vSourceName;
            meta.FSource = _fSourceName;
        }

        protected override void UpdateSourceHash()
        {
            var vSource = PatchShader(_vSourceName, ShaderType.VertexShader);
            var fSource = PatchShader(_fSourceName, ShaderType.FragmentShader);
            var gSource = _gSourceName != null ? PatchShader(_gSourceName, ShaderType.GeometryShader) : null;
            var tcSource = _tcSourceName != null ? PatchShader(_tcSourceName, ShaderType.TessControlShader) : null;
            var teSource = _teSourceName != null ? PatchShader(_teSourceName, ShaderType.TessEvaluationShader) : null;

            var builder = HashBuilder.Instance;

            builder.Reset();

            builder.Add(vSource ?? "");
            builder.Add(fSource ?? "");
            builder.Add(gSource ?? "");
            builder.Add(tcSource ?? "");
            builder.Add(teSource ?? "");
            builder.Add(OpenGLRender.Current!.Features.IsAngle ? "ANGLE" : "Native");

            _sourceHash = builder.Value();
        }

        public override void Dispose()
        {
            Vertex?.Dispose();
            Fragment?.Dispose();
            Geometry?.Dispose();
            TessControl?.Dispose();
            TessEval?.Dispose();

            Vertex = null;
            Fragment = null;
            Geometry = null;
            TessControl = null;
            TessEval = null;

            base.Dispose();
        }

        public GlShader? Vertex { get; set; }

        public GlShader? Fragment { get; set; }

        public GlShader? Geometry { get; set; }

        public GlShader? TessControl { get; set; }

        public GlShader? TessEval { get; set; }

        public bool IsBuilt => _isBuilt;
    }
}
