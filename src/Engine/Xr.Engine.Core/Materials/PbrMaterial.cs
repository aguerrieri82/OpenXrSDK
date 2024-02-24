using System.Numerics;

namespace Xr.Engine
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

    public enum PbrToneMap
    {
        TONEMAP_NONE,
        TONEMAP_ACES_NARKOWICZ,
        TONEMAP_ACES_HILL,
        TONEMAP_ACES_HILL_EXPOSURE_BOOST
    }

    public class PbrMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;


        static PbrMaterial()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "pbr/pbr.frag",
                VertexSourceName = "pbr/primitive.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = true
            };
        }


        public PbrMaterial()
        {
            Shader = SHADER;
            Debug = PbrDebugFlags.DEBUG_NONE;
            LinearOutput = true;
            //ToneMap = PbrToneMap.TONEMAP_ACES_NARKOWICZ;
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

        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            if (NormalTexture != null)
            {
                bld.AddFeature("HAS_NORMAL_MAP 1");

                bld.SetUniform("u_NormalScale", (ctx) => NormalScale);
                bld.SetUniform("u_NormalUVSet", (ctx) => NormalUVSet);
                bld.SetUniform("u_NormalSampler", (ctx) => NormalTexture, 1);
            }

            if (OcclusionTexture != null)
            {
                bld.AddFeature("HAS_OCCLUSION_MAP 1");
                bld.SetUniform("u_OcclusionStrength", (ctx) => OcclusionStrength);
                bld.SetUniform("u_OcclusionUVSet", (ctx) => OcclusionUVSet);
                bld.SetUniform("u_OcclusionSampler", (ctx) => OcclusionTexture, 2);
            }

            bld.SetUniform("u_EmissiveFactor", (ctx) => EmissiveFactor);

            if (EmissiveTexture != null)
            {
                bld.AddFeature("HAS_EMISSIVE_MAP 1");
                bld.SetUniform("u_EmissiveSampler", (ctx) => EmissiveTexture, 3);
            }

            if (MetallicRoughness != null || Type == PbrMaterialType.Metallic)
            {
                bld.AddFeature("MATERIAL_METALLICROUGHNESS 1");

                bld.SetUniform("u_MetallicFactor", (ctx) => MetallicRoughness?.MetallicFactor ?? 1);
                bld.SetUniform("u_RoughnessFactor", (ctx) => MetallicRoughness?.RoughnessFactor ?? 1);
                bld.SetUniform("u_BaseColorFactor", (ctx) => MetallicRoughness?.BaseColorFactor ?? Color.White);


                if (MetallicRoughness?.BaseColorTexture != null)
                {
                    bld.AddFeature("HAS_BASE_COLOR_MAP 1");

                    bld.SetUniform("u_BaseColorUVSet", (ctx) => MetallicRoughness.BaseColorUVSet);
                    bld.SetUniform("u_BaseColorSampler", (ctx) => MetallicRoughness.BaseColorTexture, 4);
                }

                if (MetallicRoughness?.MetallicRoughnessTexture != null)
                {
                    bld.AddFeature("HAS_METALLIC_ROUGHNESS_MAP 1");

                    bld.SetUniform("u_MetallicRoughnessUVSet", (ctx) => MetallicRoughness.MetallicRoughnessUVSet);
                    bld.SetUniform("u_MetallicRoughnessSampler", (ctx) => MetallicRoughness.MetallicRoughnessTexture, 5);
                }
            }

            else if (SpecularGlossiness != null || Type == PbrMaterialType.Specular)
            {
                bld.AddFeature("MATERIAL_SPECULARGLOSSINESS 1");

                if (SpecularGlossiness?.DiffuseTexture != null)
                {
                    bld.AddFeature("HAS_DIFFUSE_MAP 1");

                    bld.SetUniform("u_DiffuseUVSet", (ctx) => SpecularGlossiness.DiffuseUVSet);
                    bld.SetUniform("u_DiffuseSampler", (ctx) => SpecularGlossiness.DiffuseTexture, 4);
                }

                if (SpecularGlossiness?.SpecularGlossinessTexture != null)
                {
                    bld.AddFeature("HAS_SPECULAR_GLOSSINESS_MAP 1");

                    bld.SetUniform("u_SpecularGlossinessUVSet", (ctx) => SpecularGlossiness.SpecularGlossinessUVSet);
                    bld.SetUniform("u_SpecularGlossinessSampler", (ctx) => SpecularGlossiness.SpecularGlossinessTexture, 5);
                }

                bld.SetUniform("u_DiffuseFactor", (ctx) => SpecularGlossiness?.DiffuseFactor ?? Color.White);
                bld.SetUniform("u_SpecularFactor", (ctx) => SpecularGlossiness?.SpecularFactor ?? Color.White);
                bld.SetUniform("u_GlossinessFactor", (ctx) => SpecularGlossiness?.GlossinessFactor ?? 1);
            }
            else
                bld.AddFeature("MATERIAL_UNLIT 1");

            if (AlphaMode == AlphaMode.Mask)
            {
                bld.AddFeature("ALPHAMODE ALPHAMODE_MASK");
                bld.SetUniform("u_AlphaCutoff", (ctx) => AlphaCutoff);
            }
            else if (AlphaMode == AlphaMode.Opaque)
                bld.AddFeature("ALPHAMODE ALPHAMODE_OPAQUE");
            else
                bld.AddFeature("ALPHAMODE ALPHAMODE_BLEND");



            if (LinearOutput)
                bld.AddFeature("LINEAR_OUTPUT");
            else
            {
                if (ToneMap != PbrToneMap.TONEMAP_NONE)
                    bld.AddFeature(ToneMap.ToString());
            }

            bld.AddFeature("ALPHAMODE_OPAQUE 0");
            bld.AddFeature("ALPHAMODE_MASK 1");
            bld.AddFeature("ALPHAMODE_BLEND 2");

            foreach (var value in Enum.GetValues<PbrDebugFlags>())
                bld.AddFeature($"{value} {(int)value}");

            bld.AddFeature($"DEBUG {Debug}");

            if (bld.Context.Model != null)
            {
                bld.SetUniform("u_ModelMatrix", (ctx) => ctx.Model!.WorldMatrix);
                bld.SetUniform("u_NormalMatrix", (ctx) => Matrix4x4.Transpose(ctx.Model!.WorldMatrixInverse));
            }

            if (bld.Context.Camera != null)
            {
                bld.SetUniform("u_Exposure", (ctx) => ctx.Camera!.Exposure);
                bld.SetUniform("u_Camera", (ctx) => ctx.Camera!.Transform.Position);
                bld.SetUniform("u_ViewProjectionMatrix", (ctx) => ctx.Camera!.View * ctx.Camera.Projection);
            }

            if (bld.Context.Lights != null)
            {
                var lights = new List<PbrLightUniform>();

                foreach (var light in bld.Context.Lights)
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
                        var dir = directional.Forward;

                        if (lights.Count == 0)
                            dir = new Vector3(-0.5f, 0.7f, 0.5f).Normalize();
                        else if (lights.Count == 1)
                            dir = new Vector3(0.5f, -0.7f, -0.5f).Normalize();

                        lights.Add(new PbrLightUniform
                        {
                            type = PbrLightUniform.Directional,
                            color = (Vector3)directional.Color,
                            position = directional.WorldPosition,
                            direction = dir,
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

                bld.SetUniformConstStructArray("u_Lights", lights);

                bld.AddFeature("USE_PUNCTUAL 1");

                bld.AddFeature($"LIGHT_COUNT {lights.Count}");
            }
            else
                bld.AddFeature($"LIGHT_COUNT 0");


            if ((bld.Context.ActiveComponents & VertexComponent.Normal) != 0)
                bld.AddFeature("HAS_NORMAL_VEC3");

            if ((bld.Context.ActiveComponents & VertexComponent.Position) != 0)
                bld.AddFeature("HAS_POSITION_VEC3");

            if ((bld.Context.ActiveComponents & VertexComponent.Tangent) != 0)
                bld.AddFeature("HAS_TANGENT_VEC4");

            if ((bld.Context.ActiveComponents & VertexComponent.UV0) != 0)
                bld.AddFeature("HAS_TEXCOORD_0_VEC2");

            if ((bld.Context.ActiveComponents & VertexComponent.UV1) != 0)
                bld.AddFeature("HAS_TEXCOORD_1_VEC2");

            if ((bld.Context.ActiveComponents & VertexComponent.Color3) != 0)
                bld.AddFeature("HAS_COLOR_0_VEC3");

            if ((bld.Context.ActiveComponents & VertexComponent.Color4) != 0)
                bld.AddFeature("HAS_COLOR_0_VEC4");
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

        public bool LinearOutput { get; set; }

        public PbrToneMap ToneMap { get; set; }

        public PbrMaterialType Type { get; set; }

        public PbrDebugFlags Debug { get; set; }


    }
}
