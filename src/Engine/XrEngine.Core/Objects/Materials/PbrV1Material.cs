using System.Numerics;
using System.Runtime.InteropServices;
using XrMath;

namespace XrEngine
{
    public class PbrV1Material : ShaderMaterial, IColorSource, IShadowMaterial, IPbrMaterial
    {
        #region ENUMS

        public enum MaterialType
        {
            Unlit,
            Specular,
            Metallic
        }

        public enum ToneMapType
        {
            TONEMAP_NONE,
            TONEMAP_ACES_NARKOWICZ,
            TONEMAP_ACES_HILL,
            TONEMAP_ACES_HILL_EXPOSURE_BOOST,
            TONEMAP_KHR_PBR_NEUTRAL
        }

        #endregion

        #region DebugFlags

        public enum DebugFlags
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

        #endregion

        #region CameraUniforms

        [StructLayout(LayoutKind.Explicit, Size = 224)]
        public struct CameraUniforms
        {
            [FieldOffset(0)]
            public Matrix4x4 ViewMatrix;
            [FieldOffset(64)]
            public Matrix4x4 ProjectionMatrix;
            [FieldOffset(128)]
            public Matrix4x4 ViewProjectionMatrix;
            [FieldOffset(192)]
            public Vector3 Position;
            [FieldOffset(204)]
            public float Exposure;
            [FieldOffset(208)]
            public float FarPlane;
        }

        #endregion

        #region IBLTextures

        public class IBLTextures : IDisposable
        {
            public void Dispose()
            {
                LambertianEnv?.Dispose();
                LambertianEnv = null;

                GGXEnv?.Dispose();
                GGXEnv = null;

                GGXLUT?.Dispose();
                GGXLUT = null;

                CharlieEnv?.Dispose();
                CharlieEnv = null;

                CharlieLUT?.Dispose();
                CharlieLUT = null;

                Env?.Dispose();
                Env = null;
            }

            public TextureCube? LambertianEnv;

            public TextureCube? GGXEnv;
            public Texture2D? GGXLUT;

            public TextureCube? CharlieEnv;
            public Texture2D? CharlieLUT;

            public TextureCube? Env;

            public uint MipCount;
        }


        #endregion

        #region IBLUniforms

        [StructLayout(LayoutKind.Explicit, Size = 80)]
        public unsafe struct IBLUniforms
        {
            [FieldOffset(0)]
            public int MipCount;

            [FieldOffset(16)]
            public Matrix3x3Aligned EnvRotation;

            [FieldOffset(64)]
            public float EnvIntensity;
        }

        #endregion

        #region MaterialUniforms

        [StructLayout(LayoutKind.Explicit, Size = 1440)]
        public struct MaterialUniforms
        {
            [FieldOffset(0)]
            public Vector4 BaseColorFactor;

            [FieldOffset(16)]
            public int BaseColorUVSet;

            [FieldOffset(20)]
            public float MetallicFactor;

            [FieldOffset(24)]
            public float RoughnessFactor;

            [FieldOffset(28)]
            public int MetallicRoughnessUVSet;

            [FieldOffset(32)]
            public float NormalScale;

            [FieldOffset(36)]
            public int NormalUVSet;

            [FieldOffset(40)]
            public float OcclusionStrength;

            [FieldOffset(44)]

            public int OcclusionUVSet;

            [FieldOffset(48)]
            public float AlphaCutoff;

            [FieldOffset(64)]
            public Matrix3x3Aligned NormalUVTransform;

            [FieldOffset(112)]
            public Matrix3x3Aligned EmissiveUVTransform;

            [FieldOffset(160)]
            public Matrix3x3Aligned OcclusionUVTransform;

            [FieldOffset(208)]
            public Matrix3x3Aligned BaseColorUVTransform;

            [FieldOffset(256)]
            public Matrix3x3Aligned MetallicRoughnessUVTransform;

            // Sheen Material
            [FieldOffset(304)]
            public Vector3 SheenColorFactor;
            [FieldOffset(316)]
            public int SheenColorUVSet;
            [FieldOffset(320)]
            public Matrix3x3Aligned SheenColorUVTransform;

            [FieldOffset(368)]
            public float SheenRoughnessFactor;
            [FieldOffset(372)]
            public int SheenRoughnessUVSet;
            [FieldOffset(384)]
            public Matrix3x3Aligned SheenRoughnessUVTransform;

            //Specular Glossiness
            [FieldOffset(432)]
            public Vector4 DiffuseFactor;
            [FieldOffset(448)]
            public int DiffuseUVSet;

            [FieldOffset(464)]
            public Vector3 SpecularFactor;
            [FieldOffset(476)]
            public float GlossinessFactor;
            [FieldOffset(480)]
            public int SpecularGlossinessUVSet;

            [FieldOffset(496)]
            public Matrix3x3Aligned DiffuseUVTransform;
            [FieldOffset(544)]
            public Matrix3x3Aligned SpecularGlossinessUVTransform;

            //Shadow
            [FieldOffset(592)]
            public Color ShadowColor;

            // Specular Dielectrics
            [FieldOffset(800)]
            public Vector3 KHR_materials_specular_specularColorFactor;
            [FieldOffset(800)]
            public float KHR_materials_specular_specularFactor;

            // Emissive
            [FieldOffset(800)]
            public Vector3 EmissiveFactor;
            [FieldOffset(800)]
            public float EmissiveStrength;
            [FieldOffset(800)]
            public int EmissiveUVSet;

            // Clearcoat Material
            [FieldOffset(800)]
            public float ClearcoatFactor;
            [FieldOffset(800)]
            public int ClearcoatUVSet;
            [FieldOffset(800)]
            public Matrix3x3Aligned ClearcoatUVTransform;

            [FieldOffset(800)]
            public float ClearcoatRoughnessFactor;
            [FieldOffset(800)]
            public int ClearcoatRoughnessUVSet;
            [FieldOffset(800)]
            public Matrix3x3Aligned ClearcoatRoughnessUVTransform;

            [FieldOffset(800)]
            public float ClearcoatNormalScale;
            [FieldOffset(800)]
            public int ClearcoatNormalUVSet;
            [FieldOffset(800)]
            public Matrix3x3Aligned ClearcoatNormalUVTransform;

            // Specular Material
            [FieldOffset(800)]
            public int SpecularUVSet;
            [FieldOffset(800)]
            public Matrix3x3Aligned SpecularUVTransform;

            [FieldOffset(800)]
            public int SpecularColorUVSet;
            [FieldOffset(800)]
            public Matrix3x3Aligned SpecularColorUVTransform;

            // Transmission Material
            [FieldOffset(800)]
            public float TransmissionFactor;
            [FieldOffset(800)]
            public int TransmissionUVSet;
            [FieldOffset(800)]
            public Matrix3x3Aligned TransmissionUVTransform;
            [FieldOffset(800)]
            public Vector2I TransmissionFramebufferSize;

            // Volume Material
            [FieldOffset(800)]
            public Vector3 AttenuationColor;
            [FieldOffset(800)]
            public float AttenuationDistance;

            [FieldOffset(800)]
            public float ThicknessFactor;
            [FieldOffset(800)]
            public int ThicknessUVSet;
            [FieldOffset(800)]
            public Matrix3x3Aligned ThicknessUVTransform;

            // Iridescence
            [FieldOffset(800)]
            public float IridescenceIor;
            [FieldOffset(800)]
            public float IridescenceFactor;
            [FieldOffset(800)]
            public int IridescenceUVSet;
            [FieldOffset(800)]
            public Matrix3x3 IridescenceUVTransform;
            [FieldOffset(800)]
            public float IridescenceThicknessMinimum;
            [FieldOffset(800)]
            public float IridescenceThicknessMaximum;
            [FieldOffset(800)]
            public int IridescenceThicknessUVSet;
            [FieldOffset(800)]
            public Matrix3x3Aligned IridescenceThicknessUVTransform;

            // Anisotropy
            [FieldOffset(800)]
            public Vector3 Anisotropy;
            [FieldOffset(800)]
            public int AnisotropyUVSet;
            [FieldOffset(800)]
            public Matrix3x3Aligned AnisotropyUVTransform;
        }

        #endregion

        #region LightUniforms

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct LightUniforms
        {
            public const int Directional = 0;
            public const int Point = 1;
            public const int Spot = 2;

            public Vector3 Direction;
            public float Range;
            public Vector3 Color;
            public float Intensity;
            public Vector3 Position;
            public float InnerConeCos;
            public float OuterConeCos;
            public int Type;
            readonly float _Pad1;
            readonly float _Pad2;
        }

        #endregion

        #region LightListUniforms

        public struct LightListUniforms : IDynamicBuffer
        {
            static DynamicBuffer _buffer;

            public uint LightCount;

            public LightUniforms[] Lights;

            public unsafe DynamicBuffer GetBuffer()
            {
                var newSize = (sizeof(LightUniforms) * Max) + 16;

                if (_buffer.Size != newSize)
                {
                    if (_buffer.Data != 0)
                        MemoryManager.Free(_buffer.Data);

                    _buffer.Size = newSize;
                    _buffer.Data = MemoryManager.Allocate(_buffer.Size, this);
                }

                ((int*)_buffer.Data)[0] = Lights.Length;

                fixed (LightUniforms* pArray = Lights)
                {
                    var srcSpan = new Span<LightUniforms>(pArray, Lights.Length);
                    var dstSpan = new Span<LightUniforms>((LightUniforms*)(_buffer.Data + 16), Lights.Length);
                    srcSpan.CopyTo(dstSpan);
                }

                Log.Debug(this, "Update light buffer");

                return _buffer;
            }

            public static int Max { get; set; } = 16;
        }

        #endregion

        #region MetallicRoughnessData

        public class MetallicRoughnessData
        {
            public MetallicRoughnessData()
            {
                BaseColorFactor = Color.White;
                MetallicFactor = 1;
                RoughnessFactor = 1;
            }

            public Color BaseColorFactor { get; set; }

            [Range(0f, 1f, 0.01f)]
            public float MetallicFactor { get; set; }

            [Range(0f, 1f, 0.01f)]
            public float RoughnessFactor { get; set; }

            public Texture2D? BaseColorTexture { get; set; }

            public int BaseColorUVSet { get; set; }

            public Texture2D? MetallicRoughnessTexture { get; set; }

            public int MetallicRoughnessUVSet { get; set; }
        }

        #endregion

        #region SpecularGlossinessData

        public class SpecularGlossinessData
        {
            public SpecularGlossinessData()
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

        #endregion

        #region SheenData

        public class SheenData
        {
            public SheenData()
            {

            }

            public Vector3 ColorFactor { get; set; }

            public Texture2D? ColorTexture { get; set; }

            public float RoughnessFactor { get; set; }

            public Texture2D? RoughnessTexture { get; set; }
            public int ColorTextureUVSet { get; set; }
            public int RoughnessTextureUVSet { get; set; }
        }

        #endregion

        #region GlobalShaderHandler

        class GlobalShaderHandler : IShaderHandler
        {
            public bool NeedUpdateShader(UpdateShaderContext ctx)
            {
                return ctx.LastUpdate?.LightsHash != ctx.LightsHash;
            }

            public void UpdateShader(ShaderUpdateBuilder bld)
            {
                var imgLight = bld.Context.Lights?.OfType<ImageLight>().FirstOrDefault();

                var hasPunctual = bld.Context.Lights!.Any(a => a != imgLight);

                if (hasPunctual)
                    bld.AddFeature("USE_PUNCTUAL");

                if (imgLight != null)
                    bld.AddFeature("USE_IBL");

                bld.AddFeature("MAX_LIGHTS " + LightListUniforms.Max);

                bld.SetUniformBuffer("Camera", (ctx) =>
                {
                    return (CameraUniforms?)new CameraUniforms
                    {
                        Position = ctx.Camera!.WorldPosition,
                        ProjectionMatrix = ctx.Camera.Projection,
                        ViewMatrix = ctx.Camera.View,
                        ViewProjectionMatrix = ctx.Camera!.ViewProjection,
                        Exposure = ctx.Camera.Exposure,
                        FarPlane = ctx.Camera.Far
                    };
                }, 0, true);

                if (hasPunctual)
                {
                    bld.SetUniformBuffer("Lights", (ctx) =>
                    {
                        var hash = bld.Context.Lights!.Sum(a => a.Version).ToString();

                        if (ctx.CurrentBuffer!.Hash == hash)
                            return null;

                        ctx.CurrentBuffer!.Hash = hash;

                        Log.Debug(this, "Build light uniforms");

                        var lights = new List<LightUniforms>();

                        foreach (var light in bld.Context.Lights!)
                        {
                            if (light is PointLight point)
                            {
                                lights.Add(new LightUniforms
                                {
                                    Type = LightUniforms.Point,
                                    Color = (Vector3)point.Color,
                                    Position = point.WorldPosition,
                                    Intensity = point.Intensity * 10,
                                    InnerConeCos = 0,
                                    OuterConeCos = MathF.Cos(MathF.PI / 4f),
                                    Range = point.Range,
                                });
                            }
                            else if (light is DirectionalLight directional)
                            {
                                lights.Add(new LightUniforms
                                {
                                    Type = LightUniforms.Directional,
                                    Color = (Vector3)directional.Color,
                                    Direction = directional.Direction,
                                    Intensity = directional.Intensity,
                                    InnerConeCos = 1,
                                    OuterConeCos = MathF.Cos(MathF.PI / 4f),
                                    Range = -1

                                });
                            }
                            else if (light is SpotLight spot)
                            {
                                lights.Add(new LightUniforms
                                {
                                    Type = LightUniforms.Spot,
                                    Color = (Vector3)spot.Color,
                                    Position = spot.WorldPosition,
                                    Intensity = spot.Intensity,
                                    Range = spot.Range,
                                    InnerConeCos = MathF.Cos(spot.InnerConeAngle),
                                    OuterConeCos = MathF.Cos(spot.OuterConeAngle)
                                });
                            }
                        }

                        return (LightListUniforms?)new LightListUniforms
                        {
                            LightCount = (uint)lights.Count,
                            Lights = lights.ToArray()
                        };
                    }, 1, true);
                }

                if (bld.Context.ShadowMapProvider != null)
                {
                    var mode = bld.Context.ShadowMapProvider.Options.Mode;

                    if (mode != ShadowMapMode.None)
                    {
                        bld.AddFeature("USE_SHADOW_MAP");

                        if (mode == ShadowMapMode.HardSmooth)
                            bld.AddFeature("SMOOTH_SHADOW_MAP");

                        // bld.AddFeature("USE_SHADOW_SAMPLER");

                        bld.ExecuteAction((ctx, up) =>
                        {
                            up.SetUniform("uShadowMap", ctx.ShadowMapProvider!.ShadowMap!, 14);
                            up.SetUniform("uLightSpaceMatrix", ctx.ShadowMapProvider!.LightCamera!.ViewProjection);
                        });
                    }
                }


                if (imgLight != null)
                {
                    bld.SetUniformBuffer("Ibl", (ctx) =>
                    {
                        var curHash = imgLight.Version.ToString();

                        if (ctx.CurrentBuffer!.Hash == curHash)
                            return null;

                        ctx.CurrentBuffer!.Hash = imgLight.Version.ToString();

                        return (IBLUniforms?)new IBLUniforms
                        {
                            EnvIntensity = imgLight.Intensity,
                            EnvRotation = Matrix3x3.CreateRotationY(imgLight.Rotation),
                            MipCount = (int)imgLight.Textures.MipCount
                        };
                    }, 2, true);


                    bld.ExecuteAction((ctx, up) =>
                    {
                        if (imgLight.Textures?.LambertianEnv != null)
                            up.SetUniform("uLambertianEnvSampler", imgLight.Textures.LambertianEnv, 9);

                        if (imgLight.Textures?.GGXEnv != null)
                            up.SetUniform("uGGXEnvSampler", imgLight.Textures.GGXEnv, 10);

                        if (imgLight.Textures?.GGXLUT != null)
                            up.SetUniform("uGGXLUT", imgLight.Textures.GGXLUT, 11);
                        /*
                         if (imgLight.Textures?.CharlieLUT != null)
                             up.SetUniform("uCharlieLUT", imgLight.Textures.CharlieLUT, 12);

                         if (imgLight.Textures?.CharlieEnv != null)
                             up.SetUniform("uCharlieEnvSampler", imgLight.Textures.CharlieEnv, 13);*/
                    });
                }
            }
        }

        #endregion


        static readonly Shader SHADER;

        static PbrV1Material()
        {
            LinearOutput = false;
            ToneMap = ToneMapType.TONEMAP_KHR_PBR_NEUTRAL;

            SHADER = new Shader
            {
                FragmentSourceName = "pbr/pbr.frag",
                VertexSourceName = "pbr/primitive.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = true,
                UpdateHandler = new GlobalShaderHandler()
            };
        }

        public PbrV1Material()
        {
            Shader = SHADER;
            Debug = DebugFlags.DEBUG_NONE;
            UseSheen = true;
            ShadowColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }


        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.WriteObject<PbrV1Material>(this);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            container.ReadObject<PbrV1Material>(this);
        }

        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            var material = new MaterialUniforms();

            bld.SetUniformBuffer("Material", ctx => (MaterialUniforms?)material, 3, false);

            if (NormalTexture != null)
            {
                bld.AddFeature("HAS_NORMAL_MAP 1");

                material.NormalScale = NormalScale;
                material.NormalUVSet = NormalUVSet;

                if (NormalTexture.Transform != null)
                {
                    bld.AddFeature("HAS_NORMAL_UV_TRANSFORM");
                    material.NormalUVTransform = NormalTexture.Transform.Value;
                }

                bld.SetUniform("uNormalSampler", (ctx) => NormalTexture, 1);
            }

            if (OcclusionTexture != null)
            {
                bld.AddFeature("HAS_OCCLUSION_MAP 1");

                material.OcclusionStrength = OcclusionStrength;
                material.OcclusionUVSet = OcclusionUVSet;

                if (OcclusionTexture.Transform != null && !OcclusionTexture.Transform.Value.IsIdentity)
                {
                    bld.AddFeature("HAS_OCCLUSION_UV_TRANSFORM");
                    material.OcclusionUVTransform = OcclusionTexture.Transform.Value;
                }

                bld.SetUniform("uOcclusionSampler", (ctx) => OcclusionTexture, 2);
            }

            material.EmissiveFactor = EmissiveFactor;

            if (EmissiveTexture != null)
            {
                bld.AddFeature("HAS_EMISSIVE_MAP 1");
                bld.SetUniform("uEmissiveSampler", (ctx) => EmissiveTexture, 3);

                if (EmissiveTexture.Transform != null && !EmissiveTexture.Transform.Value.IsIdentity)
                {
                    bld.AddFeature("HAS_EMISSIVE_UV_TRANSFORM");
                    material.EmissiveUVTransform = EmissiveTexture.Transform.Value;
                }
            }

            if (MetallicRoughness != null || Type == MaterialType.Metallic)
            {
                bld.AddFeature("MATERIAL_METALLICROUGHNESS 1");

                material.MetallicFactor = MetallicRoughness?.MetallicFactor ?? 1;
                material.RoughnessFactor = MetallicRoughness?.RoughnessFactor ?? 1;
                material.BaseColorFactor = MetallicRoughness?.BaseColorFactor ?? Color.White;

                if (MetallicRoughness?.BaseColorTexture != null)
                {
                    bld.AddFeature("HAS_BASE_COLOR_MAP 1");

                    material.BaseColorUVSet = MetallicRoughness.BaseColorUVSet;
                    bld.SetUniform("uBaseColorSampler", (ctx) => MetallicRoughness.BaseColorTexture, 4);

                    if (MetallicRoughness.BaseColorTexture.Transform != null && !MetallicRoughness.BaseColorTexture.Transform.Value.IsIdentity)
                    {
                        bld.AddFeature("HAS_BASECOLOR_UV_TRANSFORM");
                        material.BaseColorUVTransform = MetallicRoughness.BaseColorTexture.Transform.Value;
                    }
                }

                if (MetallicRoughness?.MetallicRoughnessTexture != null)
                {
                    bld.AddFeature("HAS_METALLIC_ROUGHNESS_MAP 1");

                    material.MetallicRoughnessUVSet = MetallicRoughness.MetallicRoughnessUVSet;

                    bld.SetUniform("uMetallicRoughnessSampler", (ctx) => MetallicRoughness.MetallicRoughnessTexture, 5);

                    if (MetallicRoughness.MetallicRoughnessTexture.Transform != null && !MetallicRoughness.MetallicRoughnessTexture.Transform.Value.IsIdentity)
                    {
                        bld.AddFeature("HAS_METALLICROUGHNESS_UV_TRANSFORM 1");
                        material.MetallicRoughnessUVTransform = MetallicRoughness.MetallicRoughnessTexture.Transform.Value;
                    }

                }
            }

            else if (SpecularGlossiness != null || Type == MaterialType.Specular)
            {
                bld.AddFeature("MATERIAL_SPECULARGLOSSINESS 1");

                if (SpecularGlossiness?.DiffuseTexture != null)
                {
                    bld.AddFeature("HAS_DIFFUSE_MAP 1");

                    material.DiffuseUVSet = SpecularGlossiness.DiffuseUVSet;

                    bld.SetUniform("uDiffuseSampler", (ctx) => SpecularGlossiness.DiffuseTexture, 4);
                }

                if (SpecularGlossiness?.SpecularGlossinessTexture != null)
                {
                    bld.AddFeature("HAS_SPECULAR_GLOSSINESS_MAP 1");

                    material.SpecularGlossinessUVSet = SpecularGlossiness.SpecularGlossinessUVSet;

                    bld.SetUniform("uSpecularGlossinessSampler", (ctx) => SpecularGlossiness.SpecularGlossinessTexture, 5);
                }

                material.DiffuseFactor = SpecularGlossiness?.DiffuseFactor ?? Color.White;
                material.SpecularFactor = (Vector3)(SpecularGlossiness?.SpecularFactor ?? Color.White);
                material.GlossinessFactor = SpecularGlossiness?.GlossinessFactor ?? 1;
            }
            else
                bld.AddFeature("MATERIAL_UNLIT 1");

            if (Sheen != null && UseSheen)
            {
                bld.AddFeature("MATERIAL_SHEEN 1");

                material.SheenColorFactor = Sheen.ColorFactor;
                material.SheenRoughnessFactor = Sheen.RoughnessFactor;

                if (Sheen.ColorTexture != null)
                {

                    bld.AddFeature("HAS_SHEEN_COLOR_MAP 1");
                    material.SheenColorUVSet = Sheen.ColorTextureUVSet;
                    bld.SetUniform("uSheenColorSampler", (ctx) => Sheen.ColorTexture, 6);

                    if (Sheen.ColorTexture.Transform != null && !Sheen.ColorTexture.Transform.Value.IsIdentity)
                    {
                        bld.AddFeature("HAS_SHEENCOLOR_UV_TRANSFORM 1");
                        material.SheenColorUVTransform = Sheen.ColorTexture.Transform.Value;
                    }
                }
                if (Sheen.RoughnessTexture != null)
                {
                    bld.AddFeature("HAS_SHEEN_ROUGHNESS_MAP 1");
                    material.SheenRoughnessUVSet = Sheen.RoughnessTextureUVSet;
                    bld.SetUniform("uSheenRoughnessSampler", (ctx) => Sheen.RoughnessTexture, 7);

                    if (Sheen.RoughnessTexture.Transform != null && !Sheen.RoughnessTexture.Transform.Value.IsIdentity)
                    {
                        bld.AddFeature("HAS_SHEENROUGHNESS_UV_TRANSFORM 1");
                        material.SheenRoughnessUVTransform = Sheen.RoughnessTexture.Transform.Value;
                    }
                }

            }

            if (Alpha == AlphaMode.Mask)
            {
                bld.AddFeature("ALPHAMODE ALPHAMODE_MASK");
                material.AlphaCutoff = AlphaCutoff;
            }
            else if (Alpha == AlphaMode.Opaque)
                bld.AddFeature("ALPHAMODE ALPHAMODE_OPAQUE");
            else
            {
                if (MetallicRoughness?.BaseColorFactor != null &&
                    MetallicRoughness.BaseColorFactor.A == 0)
                {
                    bld.AddFeature("TRANSPARENT");
                }

                bld.AddFeature("ALPHAMODE ALPHAMODE_BLEND");
            }

            if (LinearOutput)
                bld.AddFeature("LINEAR_OUTPUT");
            else
            {
                if (ToneMap != ToneMapType.TONEMAP_NONE)
                    bld.AddFeature(ToneMap.ToString());
            }

            if (ReceiveShadows)
            {
                bld.AddFeature("RECEIVE_SHADOWS");
                material.ShadowColor = ShadowColor;
            }

            bld.SetUniform("uModelMatrix", (ctx) => ctx.Model!.WorldMatrix);
            bld.SetUniform("uNormalMatrix", (ctx) => ctx.Model!.NormalMatrix);

            bld.SetUniformBuffer("Material", ctx => (MaterialUniforms?)material, 3, false);

            bld.AddFeature("ALPHAMODE_OPAQUE 0");
            bld.AddFeature("ALPHAMODE_MASK 1");
            bld.AddFeature("ALPHAMODE_BLEND 2");

            bld.AddFeature($"DEBUG {Debug}");

            foreach (var value in Enum.GetValues<DebugFlags>())
                bld.AddFeature($"{value} {(int)value}");

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

        public SpecularGlossinessData? SpecularGlossiness { get; set; }

        public MetallicRoughnessData? MetallicRoughness { get; set; }

        public SheenData? Sheen { get; set; }

        public Texture2D? NormalTexture { get; set; }

        public int NormalUVSet { get; set; }


        [Range(0f, 1f, 0.01f)]
        public float NormalScale { get; set; }

        public Texture2D? OcclusionTexture { get; set; }


        [Range(0f, 1f, 0.01f)]
        public float OcclusionStrength { get; set; }

        public int OcclusionUVSet { get; set; }

        public Texture2D? EmissiveTexture { get; set; }

        public Vector3 EmissiveFactor { get; set; }


        [Range(0f, 1f, 0.01f)]
        public float AlphaCutoff { get; set; }

        public MaterialType Type { get; set; }

        public DebugFlags Debug { get; set; }

        public bool ReceiveShadows { get; set; }

        public bool UseSheen { get; set; }

        public Color ShadowColor { get; set; }

        /*
        public bool HasClearcoat { get; set; }



        public bool HasTransmission { get; set; }

        public bool HasIOR { get; set; }

        public bool HasSpecular { get; set; }

        public bool HasEmissiveStrength { get; set; }

        public bool HasVolume { get; set; }

        public bool HasIridescence { get; set; }

        public bool HasAnisotropy { get; set; }
        */

        Color IColorSource.Color
        {
            get
            {
                if (Type == MaterialType.Metallic)
                    return MetallicRoughness!.BaseColorFactor;

                if (Type == MaterialType.Specular)
                    return SpecularGlossiness!.DiffuseFactor;

                throw new NotSupportedException();
            }
            set
            {
                if (Type == MaterialType.Metallic)
                {
                    MetallicRoughness ??= new MetallicRoughnessData();
                    MetallicRoughness.BaseColorFactor = value;
                }
                else if (Type == MaterialType.Specular)
                {
                    SpecularGlossiness ??= new SpecularGlossinessData();
                    SpecularGlossiness.DiffuseFactor = value;
                }
                else
                    throw new NotSupportedException();
            }
        }


        public static bool LinearOutput { get; set; }

        public static ToneMapType ToneMap { get; set; }

        Texture2D? IPbrMaterial.ColorMap
        {
            get => MetallicRoughness?.BaseColorTexture;
            set
            {
                MetallicRoughness ??= new();
                MetallicRoughness.BaseColorTexture = value;
            }
        }
        Texture2D? IPbrMaterial.MetallicRoughnessMap
        {
            get => MetallicRoughness?.MetallicRoughnessTexture;
            set
            {
                MetallicRoughness ??= new();
                MetallicRoughness.MetallicRoughnessTexture = value;
            }
        }
        float IPbrMaterial.Metalness
        {
            get => MetallicRoughness?.MetallicFactor ?? 0;
            set
            {
                MetallicRoughness ??= new();
                MetallicRoughness.MetallicFactor = value;
            }
        }
        Texture2D? IPbrMaterial.NormalMap
        {
            get => NormalTexture;
            set
            {
                NormalTexture = value;
            }
        }

        Texture2D? IPbrMaterial.OcclusionMap
        {
            get => OcclusionTexture;
            set
            {
                OcclusionTexture = value;
            }
        }
        float IPbrMaterial.Roughness
        {
            get => MetallicRoughness?.RoughnessFactor ?? 0;
            set
            {
                MetallicRoughness ??= new();
                MetallicRoughness.RoughnessFactor = value;
            }
        }

        bool IPbrMaterial.ToneMap
        {
            get => ToneMap != ToneMapType.TONEMAP_NONE;
            set => ToneMap = value ? ToneMapType.TONEMAP_KHR_PBR_NEUTRAL : ToneMapType.TONEMAP_NONE;
        }
    }
}
