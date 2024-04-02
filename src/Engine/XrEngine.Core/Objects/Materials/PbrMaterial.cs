using System;
using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using XrEngine.Materials;
using XrMath;

namespace XrEngine
{
    public class PbrMaterial : ShaderMaterial, IColorSource
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

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct CameraUniforms
        {
            public Matrix4x4 ViewMatrix;
            public Matrix4x4 ProjectionMatrix;
            public Matrix4x4 ViewProjectionMatrix;
            public Vector3 Position;
            public float Exposure;
        }

        #endregion

        #region IBLTextures

        public class IBLTextures : IDisposable
        {
            public void Dispose()
            {
                Panorama?.Dispose();
                Panorama = null;

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

            public Texture2D? Panorama;

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

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct IBLUniforms : IDynamicBuffer
        {
            static DynamicBuffer _buffer;

            public int MipCount;
            public Matrix3x3 EnvRotation;
            public float EnvIntensity;

            public unsafe DynamicBuffer GetBuffer()
            {
                if (_buffer.Data == 0)
                {
                    _buffer.Size = 80;
                    _buffer.Data = MemoryManager.Allocate(_buffer.Size, this);
                }

                using var stream = new MemoryStream();
                using var writer = new BinaryWriter(stream);

                writer.Write(MipCount);
                stream.Position += 12;
                writer.Write(EnvRotation.M00);
                writer.Write(EnvRotation.M01);
                writer.Write(EnvRotation.M02);
                stream.Position += 4;
                writer.Write(EnvRotation.M10);
                writer.Write(EnvRotation.M11);
                writer.Write(EnvRotation.M12);
                stream.Position += 4;
                writer.Write(EnvRotation.M20);
                writer.Write(EnvRotation.M21);
                writer.Write(EnvRotation.M22);
                stream.Position += 4;
                writer.Write(EnvIntensity);
                writer.Flush();

                var bytes = stream.ToArray();
                Marshal.Copy(bytes, 0, _buffer.Data, bytes.Length);
         
                return _buffer;
            }
        }

        #endregion

        #region MaterialUniforms

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct MaterialUniforms
        {
            public Vector4 BaseColorFactor;
            public int BaseColorUVSet;

            public float MetallicFactor;
            public float RoughnessFactor;
            public int MetallicRoughnessUVSet;

            public float NormalScale;
            public int NormalUVSet;

            public float OcclusionStrength;
            public int OcclusionUVSet;

            public float AlphaCutoff;

            public Matrix3x3 NormalUVTransform;
            public Matrix3x3 EmissiveUVTransform;
            public Matrix3x3 OcclusionUVTransform;
            public Matrix3x3 BaseColorUVTransform;
            public Matrix3x3 MetallicRoughnessUVTransform;

            //Specular Glossiness
            public Vector4 DiffuseFactor;
            public int DiffuseUVSet;

            public Vector3 SpecularFactor;
            public float GlossinessFactor;
            public int SpecularGlossinessUVSet;

            public Matrix3x3 DiffuseUVTransform;
            public Matrix3x3 SpecularGlossinessUVTransform;

            // Specular Dielectrics
            public Vector3 KHR_materials_specular_specularColorFactor;
            public float KHR_materials_specular_specularFactor;

            // Emissive
            public Vector3 EmissiveFactor;
            public float EmissiveStrength;
            public int EmissiveUVSet;

            // Clearcoat Material
            public float ClearcoatFactor;
            public int ClearcoatUVSet;
            public Matrix3x3 ClearcoatUVTransform;

            public float ClearcoatRoughnessFactor;
            public int ClearcoatRoughnessUVSet;
            public Matrix3x3 ClearcoatRoughnessUVTransform;

            public float ClearcoatNormalScale;
            public int ClearcoatNormalUVSet;
            public Matrix3x3 ClearcoatNormalUVTransform;

            // Sheen Material
            public Vector3 SheenColorFactor;
            public int SheenColorUVSet;
            public Matrix3x3 SheenColorUVTransform;

            public float SheenRoughnessFactor;
            public int SheenRoughnessUVSet;
            public Matrix3x3 SheenRoughnessUVTransform;


            // Specular Material
            public int SpecularUVSet;
            public Matrix3x3 SpecularUVTransform;

            public int SpecularColorUVSet;
            public Matrix3x3 SpecularColorUVTransform;

            // Transmission Material
            public float TransmissionFactor;
            public int TransmissionUVSet;
            public Matrix3x3 TransmissionUVTransform;
            public Vector2I TransmissionFramebufferSize;

            // Volume Material
            public Vector3 AttenuationColor;
            public float AttenuationDistance;

            public float ThicknessFactor;
            public int ThicknessUVSet;
            public Matrix3x3 ThicknessUVTransform;

            // Iridescence
            public float IridescenceIor;

            public float IridescenceFactor;
            public int IridescenceUVSet;
            public Matrix3x3 IridescenceUVTransform;

            public float IridescenceThicknessMinimum;
            public float IridescenceThicknessMaximum;
            public int IridescenceThicknessUVSet;
            public Matrix3x3 IridescenceThicknessUVTransform;

            // Anisotropy
            public Vector3 Anisotropy;
            public int AnisotropyUVSet;
            public Matrix3x3 AnisotropyUVTransform;
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

                    ((int*)_buffer.Data)[0] = Lights.Length;

                    fixed (LightUniforms* pArray = Lights)
                    {
                        var srcSpan = new Span<LightUniforms>(pArray, Lights.Length);
                        var dstSpan = new Span<LightUniforms>((LightUniforms*)(_buffer.Data + 16), Lights.Length);
                        srcSpan.CopyTo(dstSpan);
                    }
                }

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

            public float MetallicFactor { get; set; }

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

        #region GlobalShaderHandler

        class GlobalShaderHandler : IShaderHandler
        {
            public bool NeedUpdateShader(UpdateShaderContext ctx, ShaderUpdate lastUpdate)
            {
                return lastUpdate.LightsVersion != ctx.LightsVersion;
            }

            public void UpdateShader(ShaderUpdateBuilder bld)
            {

                bld.SetUniformBuffer("Camera", (ctx) =>
                {
                    return new CameraUniforms
                    {
                        Position = ctx.Camera!.WorldPosition,
                        ProjectionMatrix = ctx.Camera.Projection,
                        ViewMatrix = ctx.Camera.View,
                        ViewProjectionMatrix = ctx.Camera!.View * ctx.Camera.Projection,
                        Exposure = ctx.Camera.Exposure
                    };
                }, true);

                bld.SetUniformBuffer("Lights", (ctx) =>
                {
                    if (ctx.CurrentBuffer!.Version != -1 && ctx.CurrentBuffer!.Version == ctx.LightsVersion)
                        return null;

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

                    ctx.CurrentBuffer!.Version = ctx.LightsVersion;

                    return (LightListUniforms?)new LightListUniforms
                    {
                        LightCount = (uint)lights.Count,
                        Lights = lights.ToArray()
                    };
                }, true);

                var imgLight = bld.Context.Lights?.OfType<ImageLight>().FirstOrDefault();    
                if (imgLight?.Textures != null)
                {
                    bld.AddFeature("USE_IBL");

                    bld.SetUniformBuffer("Ibl", (ctx) =>
                    {
                        if (ctx.CurrentBuffer!.Version != -1 && ctx.CurrentBuffer!.Version == imgLight.Version)
                            return null;

                        ctx.CurrentBuffer!.Version = imgLight.Version;

                        return (IBLUniforms?) new IBLUniforms
                        {
                           EnvIntensity = imgLight.Intensity,
                           EnvRotation = Matrix3x3.Rotation(imgLight.Rotation),
                           MipCount = (int)imgLight.Textures.MipCount
                        };
                    }, true);

                    bld.ExecuteAction((ctx, up) =>
                    {
                        if (imgLight.Textures.LambertianEnv != null)
                            up.SetUniform("uLambertianEnvSampler", imgLight.Textures.LambertianEnv, 6);

                        if (imgLight.Textures.GGXEnv != null)
                            up.SetUniform("uGGXEnvSampler", imgLight.Textures.GGXEnv, 7);
                        
                        if (imgLight.Textures.GGXLUT != null)
                            up.SetUniform("uGGXLUT", imgLight.Textures.GGXLUT, 8);
                        
                        if (imgLight.Textures.CharlieLUT != null)
                            up.SetUniform("uCharlieLUT", imgLight.Textures.CharlieLUT, 9);

                        if (imgLight.Textures.CharlieEnv != null)
                            up.SetUniform("uCharlieEnvSampler", imgLight.Textures.CharlieEnv, 10);                        
                    });

                }

                if (bld.Context.Lights!.Any())
                    bld.AddFeature("USE_PUNCTUAL");

                bld.AddFeature("MAX_LIGHTS " + LightListUniforms.Max);
            }
        }

        #endregion

        public static readonly IShaderHandler GlobalHandler = new GlobalShaderHandler();

        static readonly Shader SHADER;

        static PbrMaterial()
        {
            LinearOutput = true;
            ToneMap = ToneMapType.TONEMAP_KHR_PBR_NEUTRAL;

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
            Debug = DebugFlags.DEBUG_NONE;
            CastShadows = true;
        }

        public static PbrMaterial CreateDefault(Color color)
        {
            return new PbrMaterial()
            {
                Type = MaterialType.Metallic,
                Alpha = AlphaMode.Opaque,
                AlphaCutoff = 0.5f,
                Name = "Default Material",
                MetallicRoughness = new MetallicRoughnessData()
                {
                    BaseColorFactor = color,
                    MetallicFactor = 0,
                    RoughnessFactor = 0.5f
                }
            };
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.WriteObject<PbrMaterial>(this);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            container.ReadObject<PbrMaterial>(this);
        }

        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            var material = new MaterialUniforms();

            if (NormalTexture != null)
            {
                bld.AddFeature("HAS_NORMAL_MAP 1");

                material.NormalScale = NormalScale;
                material.NormalUVSet = NormalUVSet;

                bld.SetUniform("uNormalSampler", (ctx) => NormalTexture, 1);
            }

            if (OcclusionTexture != null)
            {
                bld.AddFeature("HAS_OCCLUSION_MAP 1");

                material.OcclusionStrength = OcclusionStrength;
                material.OcclusionUVSet = OcclusionUVSet;

                bld.SetUniform("uOcclusionSampler", (ctx) => OcclusionTexture, 2);
            }

            material.EmissiveFactor = EmissiveFactor;

            if (EmissiveTexture != null)
            {
                bld.AddFeature("HAS_EMISSIVE_MAP 1");
                bld.SetUniform("uEmissiveSampler", (ctx) => EmissiveTexture, 3);
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
                }

                if (MetallicRoughness?.MetallicRoughnessTexture != null)
                {
                    bld.AddFeature("HAS_METALLIC_ROUGHNESS_MAP 1");

                    material.MetallicRoughnessUVSet = MetallicRoughness.MetallicRoughnessUVSet;

                    bld.SetUniform("uMetallicRoughnessSampler", (ctx) => MetallicRoughness.MetallicRoughnessTexture, 5);
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

            if (Alpha == AlphaMode.Mask)
            {
                bld.AddFeature("ALPHAMODE ALPHAMODE_MASK");
                material.AlphaCutoff = AlphaCutoff;
            }
            else if (Alpha == AlphaMode.Opaque)
                bld.AddFeature("ALPHAMODE ALPHAMODE_OPAQUE");
            else
                bld.AddFeature("ALPHAMODE ALPHAMODE_BLEND");

            if (LinearOutput)
                bld.AddFeature("LINEAR_OUTPUT");
            else
            {
                if (ToneMap != ToneMapType.TONEMAP_NONE)
                    bld.AddFeature(ToneMap.ToString());
            }

            bld.SetUniform("uModelMatrix", (ctx) => ctx.Model!.WorldMatrix);
            bld.SetUniform("uNormalMatrix", (ctx) => Matrix4x4.Transpose(ctx.Model!.WorldMatrixInverse));

            bld.SetUniformBuffer("Material", ctx => material, false);

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

        public Texture2D? NormalTexture { get; set; }

        public int NormalUVSet { get; set; }

        public float NormalScale { get; set; }

        public Texture2D? OcclusionTexture { get; set; }

        public float OcclusionStrength { get; set; }

        public int OcclusionUVSet { get; set; }

        public Texture2D? EmissiveTexture { get; set; }

        public Vector3 EmissiveFactor { get; set; }

        public float AlphaCutoff { get; set; }

        public MaterialType Type { get; set; }

        public DebugFlags Debug { get; set; }

        public bool CastShadows { get; set; }


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

    }
}
