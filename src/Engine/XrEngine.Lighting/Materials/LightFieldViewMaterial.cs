using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using XrMath;

namespace XrEngine.Lighting
{
    public enum LightFieldViewMode
    {
        Color,
        Direction
    }

    public enum LightFieldFace
    {
        All = -1,

        NegX = 0,
        PosX = 1,

        NegY = 2,
        PosY = 3,

        NegZ = 4,
        PosZ = 5
    }

    public partial class LightFieldViewMaterial : DynamicMaterial
    {

        public LightFieldViewMaterial() :
            base("[XrEngine.Lighting]light_field.vert", "[XrEngine.Lighting]light_field.frag")
        {
            WriteDepth = false;
            UseDepth = false;
            DoubleSided = false;
            Alpha = AlphaMode.Add;
            MaxIntensity = 1f;
            Face = LightFieldFace.All;
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            if (UseSlice)
            {
                bld.AddFeature("USE_SLICE");
                bld.ExecuteAction((ctx, up) =>
                {
                    up.SetUniform("uSliceMin", SliceMin.ToRoundI());
                    up.SetUniform("uSliceMax", SliceMax.ToRoundI());
                });
            }

            bld.AddFeature($"MODE {(int)Mode}");

            bld.AddFeature($"FACE {(int)Face}");

            bld.ExecuteAction((ctx, up) =>
            {
                if (Textures == null)
                    return;

                int i = 0;
                
                foreach (var tex in Textures)
                {
                    up.LoadTexture(tex, i + 10);
                    up.SetUniform($"uLightField[{i}]", i + 10);
                    i++;
                }

                up.SetUniform("uOrigin", Origin);
                up.SetUniform("uGridSize", Size);
                up.SetUniform("uVoxelSize", VoxelSize);

                up.SetUniform("uMaxIntensity", MaxIntensity);
                up.SetUniform("uModelViewProj", ctx.PassCamera!.ViewProjection);
            });

            base.UpdateShaderMaterial(bld);
        }

        [Notify(ChangeType.Render)]
        public partial bool UseSlice { get; set; }

        public Vector3 SliceMax { get; set; }

        public Vector3 SliceMin { get; set; }

        [Range(0, 1, 0.01f)]
        public float MaxIntensity { get; set; }

        [Notify(ChangeType.Render)]
        public partial LightFieldViewMode Mode { get; set; }

        [Notify(ChangeType.Render)]
        public partial LightFieldFace Face { get; set; }

        public Vector3 Origin { get; set; }

        public Vector3I Size { get; set; }

        public float VoxelSize { get; set; }

        public IList<Texture3D>? Textures { get; set; }
    }
}
