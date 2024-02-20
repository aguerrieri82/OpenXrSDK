using SkiaSharp;
using System.Numerics;

namespace OpenXr.Engine
{
    public enum AlphaMode
    {
        Opaque,
        Mask,
        Blend
    }

    public enum PbrMaterialType
    {
        Unlit,
        Specular,
        Metallic
    }

    public class PbrLightUniform
    {
        public Vector3 direction;
        public float range;
        public Vector3 color;
        public float intensity;
        public Vector3 position;
        public float innerConeCos;
        public float outerConeCos;
        public int type;

        public const int Directional = 0;
        public const int Point = 1;
        public const int Spot = 2;
    }

    public class PbrMetallicRoughness
    {
        public PbrMetallicRoughness()
        {
            BaseColorFactor = Color.White;
            MetallicFactor = 1;
            RoughnessFactor = 1;
        }

        public Color BaseColorFactor { get; set; }

        public float MetallicFactor { get; set; }

        public float RoughnessFactor { get; set; }

        public Texture2D? BaseColorTexture { get; set; }

        public int BaseColorUVSet { get; set; }

        public Texture2D? MetallicRoughnessTexture { get; set; }

        public int MetallicRoughnessUVSet { get; set; }
    }

    public class PbrSpecularGlossiness
    {
        public PbrSpecularGlossiness()
        {
            DiffuseFactor = Color.White;
            SpecularFactor = Color.White;
            GlossinessFactor = 1;
        }

        public Color DiffuseFactor { get; set; }

        public Color SpecularFactor { get; set; }

        public float GlossinessFactor { get; set; }

        public Texture2D? DiffuseTexture { get; set; }

        public int DiffuseUVSet { get; set; }

        public Texture2D? SpecularGlossinessTexture { get; set; }

        public int SpecularGlossinessUVSet { get; set; }

    }

    public class PbrMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static PbrMaterial()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "pbr.frag",
                VertexSourceName = "primitive.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = true
            };
        }


        public PbrMaterial()
        {
            Shader = SHADER;
        }

        public static PbrMaterial CreateDefault()
        {
            return new PbrMaterial()
            {
                Type = PbrMaterialType.Metallic,
                AlphaMode = AlphaMode.Opaque,
                AlphaCutoff = 0.5f,
                Name = "Default Material",
                MetallicRoughness = new PbrMetallicRoughness()
            };
        }

        public override void UpdateUniforms(IUniformProvider obj)
        {
            if (NormalTexture != null)
            {
                obj.SetUniform("u_NormalScale", NormalScale);
                obj.SetUniform("u_NormalUVSet", NormalUVSet);
                obj.SetUniform("u_NormalSampler", NormalTexture, 1);
            }

            if (OcclusionTexture != null)
            {
                obj.SetUniform("u_OcclusionStrength", OcclusionStrength);
                obj.SetUniform("u_OcclusionUVSet", OcclusionUVSet);
                obj.SetUniform("u_OcclusionSampler", OcclusionTexture, 2);
            }

            obj.SetUniform("u_EmissiveFactor", EmissiveFactor);

            if (EmissiveTexture != null)
            {
                obj.SetUniform("u_EmissiveSampler", EmissiveTexture, 3);
            }

            if (MetallicRoughness != null || Type == PbrMaterialType.Metallic)
            {
                obj.SetUniform("u_MetallicFactor", MetallicRoughness?.MetallicFactor ?? 1);
                obj.SetUniform("u_RoughnessFactor", MetallicRoughness?.RoughnessFactor ?? 1);
                obj.SetUniform("u_BaseColorFactor", MetallicRoughness?.BaseColorFactor ?? Color.White);


                if (MetallicRoughness?.BaseColorTexture != null)
                {
                    obj.SetUniform("u_BaseColorUVSet", MetallicRoughness.BaseColorUVSet);
                    obj.SetUniform("u_BaseColorSampler", MetallicRoughness.BaseColorTexture, 4);
                }

                if (MetallicRoughness?.MetallicRoughnessTexture != null)
                {
                    obj.SetUniform("u_MetallicRoughnessUVSet", MetallicRoughness.MetallicRoughnessUVSet);
                    obj.SetUniform("u_MetallicRoughnessSampler", MetallicRoughness.MetallicRoughnessTexture, 5);
                }
            }

            else if (SpecularGlossiness != null || Type == PbrMaterialType.Specular)
            {

                if (SpecularGlossiness?.DiffuseTexture != null)
                {
                    obj.SetUniform("u_DiffuseUVSet", SpecularGlossiness.DiffuseUVSet);
                    obj.SetUniform("u_DiffuseSampler", SpecularGlossiness.DiffuseTexture, 4);
                }

                if (SpecularGlossiness?.SpecularGlossinessTexture != null)
                {
                    obj.SetUniform("u_SpecularGlossinessUVSet", SpecularGlossiness.SpecularGlossinessUVSet);
                    obj.SetUniform("u_SpecularGlossinessSampler", SpecularGlossiness.SpecularGlossinessTexture, 5);
                }

                obj.SetUniform("u_DiffuseFactor", SpecularGlossiness?.DiffuseFactor ?? Color.White);
                obj.SetUniform("u_SpecularFactor", SpecularGlossiness?.SpecularFactor ?? Color.White);
                obj.SetUniform("u_GlossinessFactor", SpecularGlossiness?.GlossinessFactor ?? 1);
            }

            if (AlphaMode== AlphaMode.Mask)
            {
                obj.SetUniform("u_AlphaCutoff", AlphaCutoff);
            }

            base.UpdateUniforms(obj);
        }

        public override void ExtractFeatures(IFeatureList features)
        {
            features.AddFeature("DEBUG DEBUG_NONE");

            if (NormalTexture != null)
            {
                features.AddFeature("HAS_NORMAL_MAP 1");
            }

            if (OcclusionTexture != null)
            {
                features.AddFeature("HAS_OCCLUSION_MAP 1");
            }

            if (EmissiveTexture != null)
            {
                features.AddFeature("HAS_EMISSIVE_MAP 1");
            }

            if (MetallicRoughness != null || Type == PbrMaterialType.Metallic)
            {
                features.AddFeature("MATERIAL_METALLICROUGHNESS 1");

                if (MetallicRoughness?.BaseColorTexture != null)
                {
                    features.AddFeature("HAS_BASE_COLOR_MAP 1");
                }

                if (MetallicRoughness?.MetallicRoughnessTexture != null)
                {
                    features.AddFeature("HAS_METALLIC_ROUGHNESS_MAP 1");
                }
            }

            else if (SpecularGlossiness != null || Type == PbrMaterialType.Specular)
            {
                features.AddFeature("MATERIAL_SPECULARGLOSSINESS 1");

                if (SpecularGlossiness?.DiffuseTexture != null)
                {
                    features.AddFeature("HAS_DIFFUSE_MAP 1");
                }

                if (SpecularGlossiness?.SpecularGlossinessTexture != null)
                {
                    features.AddFeature("HAS_SPECULAR_GLOSSINESS_MAP 1");
                }
            }
            else
            {
                features.AddFeature("MATERIAL_UNLIT 1");
            }

            if (AlphaMode == AlphaMode.Mask)
            {
                features.AddFeature("ALPHAMODE ALPHAMODE_MASK");
            }
            else if (AlphaMode == AlphaMode.Opaque)
                features.AddFeature("ALPHAMODE ALPHAMODE_OPAQUE");
            else
                features.AddFeature("ALPHAMODE ALPHAMODE_BLEND");
        }

        public PbrSpecularGlossiness? SpecularGlossiness { get; set; }

        public PbrMetallicRoughness? MetallicRoughness { get; set; }

        public Texture2D? NormalTexture { get; set; }

        public int NormalUVSet { get; set; }

        public float NormalScale { get; set; }

        public Texture2D? OcclusionTexture { get; set; }

        public float OcclusionStrength { get; set; }

        public int OcclusionUVSet { get; set; }

        public Texture2D? EmissiveTexture { get; set; }

        public Vector3 EmissiveFactor { get; set; }

        public float AlphaCutoff { get; set; }

        public AlphaMode AlphaMode { get; set; }

        /*
        public bool HasClearcoat { get; set; }

        public bool HasSheen { get; set; }

        public bool HasTransmission { get; set; }

        public bool HasIOR { get; set; }

        public bool HasSpecular { get; set; }

        public bool HasEmissiveStrength { get; set; }

        public bool HasVolume { get; set; }

        public bool HasIridescence { get; set; }

        public bool HasAnisotropy { get; set; }
        */

        public PbrMaterialType Type { get; set; }


    }
}
