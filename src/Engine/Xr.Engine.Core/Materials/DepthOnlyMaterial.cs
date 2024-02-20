﻿namespace OpenXr.Engine
{
    public class DepthOnlyMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static DepthOnlyMaterial()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "color.frag",
                VertexSourceName = "standard.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = false
            };
        }


        public DepthOnlyMaterial()
            : base()
        {
            _shader = SHADER;
            WriteColor = false;
        }

        public override void UpdateUniforms(IUniformProvider obj)
        {
            obj.SetUniform("color", Color.Transparent);
        }

        public static readonly DepthOnlyMaterial Instance = new DepthOnlyMaterial();
    }
}
