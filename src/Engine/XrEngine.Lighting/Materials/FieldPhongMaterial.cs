using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Text;
using XrMath;

namespace XrEngine.Lighting
{
    public class FieldPhongMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static FieldPhongMaterial()
        {
            SHADER = new StandardVertexShader
            {
                FragmentSourceName = "[XrEngine.Lighting]field_phong.frag",
            };
        }

        public FieldPhongMaterial() 
        {
            Shader = SHADER;
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                if (Textures == null)
                    return;

                int i = 0;

                foreach (var tex in Textures)
                {
                    up.LoadTexture(tex, i);
                    up.SetUniform($"uLightField[{i}]", i);
                    i++;
                }

                up.SetUniform("uCameraPosition", ctx.PassCamera!.WorldPosition);
                up.SetUniform("uLightFieldOrigin", Origin);
                up.SetUniform("uGridSize", Size);
                up.SetUniform("uVoxelSize", VoxelSize);
            });

            base.UpdateShaderMaterial(bld);
        }

        public Vector3 Origin { get; set; }

        public Vector3I Size { get; set; }

        public float VoxelSize { get; set; }

        public IList<Texture3D>? Textures { get; set; }

    }
}
