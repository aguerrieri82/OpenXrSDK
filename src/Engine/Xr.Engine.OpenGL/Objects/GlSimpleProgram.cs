﻿#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Security.Cryptography;
using System.Text;
using Xr.Engine.OpenGL;
using System.Diagnostics.CodeAnalysis;


namespace OpenXr.Engine.OpenGL
{
    public partial class GlSimpleProgram : GlProgram
    {
        readonly string _vSource;
        readonly string _fSource;


        public GlSimpleProgram(GL gl, string vSource, string fSource, Func<string, string> resolver, GlRenderOptions options)
            : base(gl, resolver, options)
        {
            _fSource = fSource;
            _vSource = vSource;
            _programId = Convert.ToBase64String(MD5.HashData(Encoding.UTF8.GetBytes(vSource + fSource)));
        }

        [MemberNotNull(nameof(Vertex))]
        [MemberNotNull(nameof(Fragment))]
        protected override void Build()
        {
            Vertex = new GlShader(_gl, ShaderType.VertexShader, PatchShader(_vSource, ShaderType.VertexShader));
            Fragment = new GlShader(_gl, ShaderType.FragmentShader, PatchShader(_fSource, ShaderType.FragmentShader));
            Create(Vertex, Fragment);
        }

        public GlShader? Vertex { get; set; }

        public GlShader? Fragment { get; set; }
    }
}
