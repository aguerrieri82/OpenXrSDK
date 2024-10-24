﻿#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics.CodeAnalysis;


namespace XrEngine.OpenGL
{
    public partial class GlSimpleProgram : GlBaseProgram
    {
        readonly string _vSourceName;
        readonly string _fSourceName;
        readonly string? _gSourceName;

        public GlSimpleProgram(GL gl, string vSource, string fSource, Func<string, string> resolver)
            : base(gl, resolver)
        {
            _fSourceName = fSource;
            _vSourceName = vSource;
        }

        public GlSimpleProgram(GL gl, string vSource, string fSource, string? gSource, Func<string, string> resolver)
            : this(gl, vSource, fSource, resolver)
        {
            _gSourceName = gSource;
        }


        [MemberNotNull(nameof(Vertex))]
        [MemberNotNull(nameof(Fragment))]
        public override void Build()
        {
            Log.Debug(this, "Building program {0}...", _handle);

            var vSource = PatchShader(_vSourceName, ShaderType.VertexShader);
            var fSource = PatchShader(_fSourceName, ShaderType.FragmentShader);
            var gSource = _gSourceName != null ? PatchShader(_gSourceName, ShaderType.GeometryShader) : null;

            //vSource = ShaderPreprocessor.ParseShader(vSource);

            Vertex = GlShader.GetOrCreate(_gl, ShaderType.VertexShader, vSource);
            Fragment = GlShader.GetOrCreate(_gl, ShaderType.FragmentShader, fSource);

            if (gSource != null)
                Geometry = GlShader.GetOrCreate(_gl, ShaderType.GeometryShader, gSource);

            Create(Vertex, Fragment, Geometry?.Handle ?? 0);

            _values.Clear();
            _locations.Clear();
            for (var i = 0; i < _boundBuffers.Length; i++)
                _boundBuffers[i] = 0;

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
