using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class StandardMaterial : ShaderMaterial
    {
        static Shader _shader;

        static StandardMaterial()
        {
            _shader = new Shader();
            _shader.FragmentSource = Embedded.GetString("standard_fs.glsl");
            _shader.VertexSource = Embedded.GetString("standard_vs.glsl");
        }

        public StandardMaterial()
        {
            Specular.Rgb(0.5f);
            Shininess = 32f;
            Shader = _shader;
        }

        public override void UpdateUniforms(IUniformProvider obj)
        {
            obj.SetUniform("material.ambient", Ambient);
            obj.SetUniform("material.diffuse", Color);
            obj.SetUniform("material.specular", Specular);
            obj.SetUniform("material.shininess", 32.0f);

            base.UpdateUniforms(obj);
        }

        public Color Ambient { get; set; }

        public Color Specular { get; set; }

        public float Shininess { get; set; }

    }
}
