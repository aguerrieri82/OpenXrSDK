using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine;

namespace XrSamples.Earth
{
    public class StarDomeMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static StarDomeMaterial()
        {
            SHADER = new StandardVertexShader
            {
                FragmentSourceName = "star_dome.frag",
                IsLit = false,
                Resolver = str =>
                {
                    if (str.EndsWith(".frag"))
                        return Embedded.GetString(str);
                    return Embedded.GetString<Material>(str);
                }
            };
        }
        public StarDomeMaterial()
            : base()
        {
            _shader = SHADER;

            DoubleSided = true;
            Stars = AssetLoader.Instance.Load<Texture2D>("res://asset/starmap_16k.tif");
            Grid = AssetLoader.Instance.Load<Texture2D>("res://asset/celestial_grid.tif");
            Constellations = AssetLoader.Instance.Load<Texture2D>("res://asset/constellation_figures.tif");
            Exposure = 1;
            Transparency = 1;
        }

        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            if (ShowGrid)
            {
                bld.AddFeature("SHOW_GRID");
                bld.LoadTexture(ctx => Grid, 1);
            }
            if (ShowConstellations)
            {
                bld.AddFeature("SHOW_CONST");
                bld.LoadTexture(ctx => Constellations, 2);
            }

            bld.ExecuteAction((ctx, up) =>
            {
                up.LoadTexture(Stars, 0);
                up.SetUniform("uModel", ctx.Model!.WorldMatrix);
                up.SetUniform("uNormalMatrix", ctx.Model!.NormalMatrix);
                up.SetUniform("uExposure", Exposure);
                up.SetUniform("uOffset", Offset);
                up.SetUniform("uTransparency", Transparency);
            });
        }

        [Range(0, 1, 0.01f)]
        public float Transparency { get; set; }

        [Range(0, 10, 0.1f)]
        public float Exposure { get; set; }

        [Range(0, -5, 0.01f)]
        public float Offset { get; set; }

        public bool ShowGrid { get; set; }

        public bool ShowConstellations { get; set; }

        public Texture2D Stars { get; set; }

        public Texture2D Grid { get; set; }

        public Texture2D Constellations { get; set; }

    }
}
