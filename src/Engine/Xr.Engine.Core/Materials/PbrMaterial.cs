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

    public enum PbrDebugFlags
    {
        DEBUG_NORMAL_SHADING,
        DEBUG_NORMAL_TEXTURE,
        DEBUG_NORMAL_GEOMETRY,
        DEBUG_TANGENT,
        DEBUG_BITANGENT,
        DEBUG_ALPHA,
        DEBUG_UV_0,
        DEBUG_UV_1,
        DEBUG_OCCLUSION,
        DEBUG_EMISSIVE,
        DEBUG_METALLIC_ROUGHNESS,
        DEBUG_BASE_COLOR,
        DEBUG_ROUGHNESS,
        DEBUG_METALLIC,
        DEBUG_CLEARCOAT,
        DEBUG_CLEARCOAT_FACTOR,
        DEBUG_CLEARCOAT_ROUGHNESS,
        DEBUG_CLEARCOAT_NORMAL,
        DEBUG_SHEEN,
        DEBUG_SHEEN_COLOR,
        DEBUG_SHEEN_ROUGHNESS,
        DEBUG_SPECULAR,
        DEBUG_SPECULAR_FACTOR,
        DEBUG_SPECULAR_COLOR,
        DEBUG_TRANSMISSION_VOLUME,
        DEBUG_TRANSMISSION_FACTOR,
        DEBUG_VOLUME_THICKNESS,
        DEBUG_IRIDESCENCE,
        DEBUG_IRIDESCENCE_FACTOR,
        DEBUG_IRIDESCENCE_THICKNESS,
        DEBUG_ANISOTROPIC_STRENGTH,
        DEBUG_ANISOTROPIC_DIRECTION,
        DEBUG_NONE
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
            Debug = PbrDebugFlags.DEBUG_NONE;
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

        public override void UpdateShader(UpdateShaderContext ctx, IUniformProvider up, IFeatureList fl)
        {
            if (NormalTexture != null)
            {
                fl.AddFeature("HAS_NORMAL_MAP 1");

                up.SetUniform("u_NormalScale", NormalScale);
                up.SetUniform("u_NormalUVSet", NormalUVSet);
                up.SetUniform("u_NormalSampler", NormalTexture, 1);
            }

            if (OcclusionTexture != null)
            {
                fl.AddFeature("HAS_OCCLUSION_MAP 1");
                up.SetUniform("u_OcclusionStrength", OcclusionStrength);
                up.SetUniform("u_OcclusionUVSet", OcclusionUVSet);
                up.SetUniform("u_OcclusionSampler", OcclusionTexture, 2);
            }

            up.SetUniform("u_EmissiveFactor", EmissiveFactor);

            if (EmissiveTexture != null)
            {
                fl.AddFeature("HAS_EMISSIVE_MAP 1");
                up.SetUniform("u_EmissiveSampler", EmissiveTexture, 3);
            }

            if (MetallicRoughness != null || Type == PbrMaterialType.Metallic)
            {
                fl.AddFeature("MATERIAL_METALLICROUGHNESS 1");

                up.SetUniform("u_MetallicFactor", MetallicRoughness?.MetallicFactor ?? 1);
                up.SetUniform("u_RoughnessFactor", MetallicRoughness?.RoughnessFactor ?? 1);
                up.SetUniform("u_BaseColorFactor", MetallicRoughness?.BaseColorFactor ?? Color.White);


                if (MetallicRoughness?.BaseColorTexture != null)
                {
                    fl.AddFeature("HAS_BASE_COLOR_MAP 1");

                    up.SetUniform("u_BaseColorUVSet", MetallicRoughness.BaseColorUVSet);
                    up.SetUniform("u_BaseColorSampler", MetallicRoughness.BaseColorTexture, 4);
                }

                if (MetallicRoughness?.MetallicRoughnessTexture != null)
                {
                    fl.AddFeature("HAS_METALLIC_ROUGHNESS_MAP 1");

                    up.SetUniform("u_MetallicRoughnessUVSet", MetallicRoughness.MetallicRoughnessUVSet);
                    up.SetUniform("u_MetallicRoughnessSampler", MetallicRoughness.MetallicRoughnessTexture, 5);
                }
            }

            else if (SpecularGlossiness != null || Type == PbrMaterialType.Specular)
            {
                fl.AddFeature("MATERIAL_SPECULARGLOSSINESS 1");

                if (SpecularGlossiness?.DiffuseTexture != null)
                {
                    fl.AddFeature("HAS_DIFFUSE_MAP 1");

                    up.SetUniform("u_DiffuseUVSet", SpecularGlossiness.DiffuseUVSet);
                    up.SetUniform("u_DiffuseSampler", SpecularGlossiness.DiffuseTexture, 4);
                }

                if (SpecularGlossiness?.SpecularGlossinessTexture != null)
                {
                    fl.AddFeature("HAS_SPECULAR_GLOSSINESS_MAP 1");

                    up.SetUniform("u_SpecularGlossinessUVSet", SpecularGlossiness.SpecularGlossinessUVSet);
                    up.SetUniform("u_SpecularGlossinessSampler", SpecularGlossiness.SpecularGlossinessTexture, 5);
                }

                up.SetUniform("u_DiffuseFactor", SpecularGlossiness?.DiffuseFactor ?? Color.White);
                up.SetUniform("u_SpecularFactor", SpecularGlossiness?.SpecularFactor ?? Color.White);
                up.SetUniform("u_GlossinessFactor", SpecularGlossiness?.GlossinessFactor ?? 1);
            }
            else
                fl.AddFeature("MATERIAL_UNLIT 1");

            if (AlphaMode== AlphaMode.Mask)
            {
                fl.AddFeature("ALPHAMODE ALPHAMODE_MASK");
                up.SetUniform("u_AlphaCutoff", AlphaCutoff);
            }
            else if (AlphaMode == AlphaMode.Opaque)
                fl.AddFeature("ALPHAMODE ALPHAMODE_OPAQUE");
            else
                fl.AddFeature("ALPHAMODE ALPHAMODE_BLEND");


            if (ctx.Model != null)
            {
                up.SetUniform("u_ModelMatrix", ctx.Model.WorldMatrix);
                up.SetUniform("u_NormalMatrix", Matrix4x4.Transpose(ctx.Model.WorldMatrixInverse));
            }

            if (ctx.Camera != null)
            {
                up.SetUniform("u_Exposure", ctx.Camera.Exposure);
                up.SetUniform("u_Camera", ctx.Camera.Transform.Position);
                up.SetUniform("u_ViewProjectionMatrix", ctx.Camera.Transform.Matrix * ctx.Camera.Projection);
            }

            int lightCount = 0;
            if (ctx.Lights != null)
            {
                var lights = new List<PbrLightUniform>();

                foreach (var light in ctx.Lights)
                {
                    if (light is PointLight point)
                    {
                        lights.Add(new PbrLightUniform
                        {
                            type = PbrLightUniform.Point,
                            color = (Vector3)point.Color,
                            position = point.WorldPosition,
                            intensity = point.Intensity * 10,
                            innerConeCos = 0,
                            outerConeCos = MathF.Cos(MathF.PI / 4f),
                            range = point.Range,
                        });
                    }
                    else if (light is DirectionalLight directional)
                    {
                        lights.Add(new PbrLightUniform
                        {
                            type = PbrLightUniform.Directional,
                            color = (Vector3)directional.Color,
                            position = directional.WorldPosition,
                            direction = directional.Forward,
                            intensity = directional.Intensity,
                            innerConeCos = 1,
                            outerConeCos = MathF.Cos(MathF.PI / 4f),
                            range = -1

                        });
                    }
                    else if (light is SpotLight spot)
                    {
                        lights.Add(new PbrLightUniform
                        {
                            type = PbrLightUniform.Spot,
                            color = (Vector3)spot.Color,
                            position = spot.WorldPosition,
                            direction = spot.Forward,
                            intensity = spot.Intensity,
                            range = spot.Range,
                            innerConeCos = MathF.Cos(spot.InnerConeAngle),
                            outerConeCos = MathF.Cos(spot.OuterConeAngle)
                        });
                    }
                }

                up.SetUniformStructArray("u_Lights", lights);
            }

            if (lightCount > 0)
                fl.AddFeature("USE_PUNCTUAL 1");

            fl.AddFeature($"LIGHT_COUNT {lightCount}");

            //fl.AddFeature("LINEAR_OUTPUT");

            fl.AddFeature("ALPHAMODE_OPAQUE 0");
            fl.AddFeature("ALPHAMODE_MASK 1");
            fl.AddFeature("ALPHAMODE_BLEND 2");

            foreach (var value in Enum.GetValues<PbrDebugFlags>())
                fl.AddFeature($"{value} {(int)value}");

            fl.AddFeature($"DEBUG {Debug}");

            if ((ctx.ActiveComponents & VertexComponent.Normal) != 0)
                fl.AddFeature("HAS_NORMAL_VEC3");

            if ((ctx.ActiveComponents & VertexComponent.Position) != 0)
                fl.AddFeature("HAS_POSITION_VEC3");

            if ((ctx.ActiveComponents & VertexComponent.Tangent) != 0)
                fl.AddFeature("HAS_TANGENT_VEC4");

            if ((ctx.ActiveComponents & VertexComponent.UV0) != 0)
                fl.AddFeature("HAS_TEXCOORD_0_VEC2");

            if ((ctx.ActiveComponents & VertexComponent.UV1) != 0)
                fl.AddFeature("HAS_TEXCOORD_1_VEC2");

            if ((ctx.ActiveComponents & VertexComponent.Color3) != 0)
                fl.AddFeature("HAS_COLOR_0_VEC3");

            if ((ctx.ActiveComponents & VertexComponent.Color4) != 0)
                fl.AddFeature("HAS_COLOR_0_VEC4");
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

        public PbrDebugFlags Debug { get; set; }


    }
}
