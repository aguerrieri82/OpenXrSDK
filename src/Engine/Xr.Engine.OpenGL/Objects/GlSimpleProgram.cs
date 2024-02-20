#if GLES
using Silk.NET.OpenGLES;

#else
using Silk.NET.OpenGL;
using System.Diagnostics.CodeAnalysis;

#endif

using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Xr.Engine.OpenGL;


namespace OpenXr.Engine.OpenGL
{
    public partial class GlSimpleProgram : GlProgram
    {
        readonly string _vSource;
        readonly string _fSource;
        protected PointLight? _point;
        protected AmbientLight? _ambient;

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

        public override void BeginEdit()
        {
            _ambient = null;
            _point = null;  
            base.BeginEdit();
        }

        public override void SetAmbient(AmbientLight light)
        {
            _ambient = light;
        }

        public override void AddLight(PointLight point)
        {
            _point = point;
        }

        public override void ConfigureLights()
        {
            if (_point != null)
            {
                SetUniform("light.diffuse", (Vector3)_point.Color * _point.Intensity);
                SetUniform("light.position", _point.WorldPosition);
                SetUniform("light.specular", (Vector3)_point.Specular);
            }
            if (_ambient != null)
            {
                SetUniform("light.ambient", (Vector3)_ambient.Color * _ambient.Intensity);
            }
        }

        public override void AddLight(DirectionalLight directional)
        {

        }

        public override void AddLight(SpotLight spot)
        {

        }

        public override void SetCamera(Camera camera)
        {
            SetUniform("uView", camera.Transform.Matrix);
            SetUniform("uProjection", camera.Projection);
            SetUniform("uViewPos", camera.Transform.Position, true);
        }

        public GlShader? Vertex { get; set; }

        public GlShader? Fragment { get; set; }
    }
}
