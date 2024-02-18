#if GLES
using Silk.NET.OpenGLES;

#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;
using System.Text;
using Xr.Engine.OpenGL;


namespace OpenXr.Engine.OpenGL
{
    public class GlSimpleProgram : GlProgram
    {

        public GlSimpleProgram(GL gl, string vSource, string fSource, GlRenderOptions options) 
            : base(gl, options)
        {
            Vertex = new GlShader(gl, ShaderType.VertexShader, PatchShader(vSource, ShaderType.VertexShader));
            Fragment = new GlShader(gl, ShaderType.FragmentShader, PatchShader(fSource, ShaderType.FragmentShader));
            Create(Vertex.Handle, Fragment.Handle);
        }

        public virtual void SetAmbient(AmbientLight light)
        {
            SetUniform("light.ambient", (Vector3)light.Color * light.Intensity);
        }

        public virtual void AddPointLight(PointLight point)
        {
            var wordPos = Vector3.Transform(point.Transform.Position, point.WorldMatrix);

            SetUniform("light.diffuse", (Vector3)point.Color * point.Intensity);
            SetUniform("light.position", wordPos);
            SetUniform("light.specular", (Vector3)point.Specular);
        }

        public virtual void SetCamera(Camera camera)
        {
            SetUniform("uView", camera.Transform.Matrix);

            SetUniform("uProjection", camera.Projection);

            SetUniform("viewPos", camera.Transform.Position, true);
        }

        protected string PatchShader(string source, ShaderType shaderType)
        {
            var builder = new StringBuilder();

            builder.Append("#version ")
               .Append(_options.ShaderVersion!)
               .Append("\n");

            if (shaderType == ShaderType.VertexShader)
            {
                if (_options.ShaderExtensions != null)
                {
                    foreach (var ext in _options.ShaderExtensions)
                        builder.Append($"#extension {ext} : require\n");
                }
            }

            var precision = _options.FloatPrecision switch
            {
                ShaderPrecision.Medium => "mediump",
                ShaderPrecision.High => "highp",
                ShaderPrecision.Low => "lowp",
                _ => throw new NotSupportedException()
            };

            builder.Append("precision ").Append(precision).Append(" float;\n");

            PatchShader(source, shaderType, builder);

            builder.Append("\n\n").Append(source);

            return builder.ToString();
        }

        protected virtual void PatchShader(string source, ShaderType shaderType, StringBuilder builder)
        {

        }


        public GlShader Vertex { get; set; }

        public GlShader Fragment { get; set; }

    }
}
