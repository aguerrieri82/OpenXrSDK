
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using XrMath;
using XrMath.Entities;

namespace XrEngine
{
    public enum PbrDebug
    {
        None = 0,
        Uv = 1,
        Normal = 2,
        Tangent = 3,
        Bitangent = 4,
        Metalness = 5,
        Roughness = 6,
        Irradiance = 7,
        FieldDir = 8,
        FieldRad = 9,
        Transmission = 10
    }

    public enum UseLightFieldMode
    {
        None,
        Self,
        SelfOmni,
        Full
    }

    public class PbrMaterial : ShaderMaterial, IColorSource, IShadowMaterial, IPbrMaterial, IEnvDepthMaterial, IHeightMaterial, ITransmissionMaterial
    {
        #region CATEGORIES

        const string Surface = nameof(Surface);
        const string Textures = nameof(Textures);
        const string Rendering = nameof(Rendering);
        const string Volume = nameof(Volume);
        const string Iridescence = nameof(Iridescence);

        #endregion

        #region MaterialUniforms

        [StructLayout(LayoutKind.Explicit, Size = 160)]
        public struct MaterialUniforms
        {
            [FieldOffset(0)]
            public Vector4 Color;

            [FieldOffset(16)]
            public float Metalness;

            [FieldOffset(20)]
            public float Roughness;

            [FieldOffset(32)]
            public Vector4x3 TexTransform;

            [FieldOffset(80)]
            public float OcclusionStrength;

            [FieldOffset(96)]
            public Vector4 ShadowColor;

            [FieldOffset(112)]
            public float NormalScale;

            [FieldOffset(116)]
            public float AlphaCutoff;

            [FieldOffset(128)]
            public Vector4 EmissiveColor;

            [FieldOffset(144)]
            public float PlanarReflectionStrength;

            [FieldOffset(148)]
            public float PlanarReflectionLevel;

            [FieldOffset(152)]
            public float AlphaSpecularScale;

            [FieldOffset(156)]
            public float Transmission;
        }

        #endregion

        #region LightListUniforms

        [InlineArray(LightListUniforms.Max)]
        public struct LightUniformsArray
        {
            private LightUniforms _element0;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct LightListUniforms
        {
            public const int Max = 10;

            [FieldOffset(0)]
            public uint Count;

            [FieldOffset(16)]
            public LightUniformsArray Lights;
        }

        #endregion

        #region IblUniforms

        [StructLayout(LayoutKind.Explicit, Size = 80)]
        public struct IblUniforms
        {
            [FieldOffset(0)]
            public float SpecularTextureLevels;

            [FieldOffset(4)]
            public float Intensity;

            [FieldOffset(8)]
            public float ShadowStrength;

            [FieldOffset(16)]
            public Vector3 Color;

            [FieldOffset(32)]
            public Vector4x3 Transform;
        }

        #endregion

        #region LightUniforms

        [StructLayout(LayoutKind.Explicit, Size = 112)]
        public struct LightUniforms
        {
            [FieldOffset(0)]
            public uint Type;

            [FieldOffset(16)]
            public Vector3 Position;

            [FieldOffset(32)]
            public Vector3 Direction;

            [FieldOffset(48)]
            public Vector3 Color; // radiance

            [FieldOffset(60)]
            public float Range; // radius

            [FieldOffset(64)]
            public float OutConeCos;

            [FieldOffset(68)]
            public float InConeCos;

            [FieldOffset(80)]
            public Vector3 AxisX;

            [FieldOffset(92)]
            public float HalfWidth;

            [FieldOffset(96)]
            public Vector3 AxisY;

            [FieldOffset(108)]
            public float HalfHeight;
        }

        #endregion

        #region PbrShader

        public class PbrShader : StandardShader, IShaderHandler
        {
            ILightFieldProvider? _lightFieldProvider;
            readonly PerspectiveCamera _depthCamera = new();

            public PbrShader()
            {
                UseDepthCulling = true;
                UseMotionVectors = true;

            }

            public override bool NeedUpdateShader(UpdateShaderContext ctx)
            {
                var ibl = ctx.Lights?.OfType<ImageLight>().FirstOrDefault();

                return ctx.LastGlobalUpdate?.LightsHash != ctx.LightsHash ||
                       ctx.LastGlobalUpdate?.ShaderVersion != Version ||
                       _tracker.IsChanged(() => ibl?.Version ?? -1) ||
                       base.NeedUpdateShader(ctx);
            }

            protected override void UpdateShaderGlobal(ShaderUpdateBuilder bld)
            {
                var imgLight = bld.Context.Lights?.OfType<ImageLight>().FirstOrDefault();

                var hasPunctual = bld.Context.Lights != null && bld.Context.Lights.Any(a => a != imgLight);

                bld.AddFeature("PBR_V2");

                var globalToneMap = false;

                if (Context.TryRequire<IToneMapper>(out var mapper))
                    globalToneMap = mapper.IsGlobal;

                if (ToneMap != ToneMapMode.None && !globalToneMap)
                    bld.AddFeature($"TONE_MAP {(int)ToneMap}");

                if (UseDepthCulling && bld.Context.DepthCullProvider?.IsActive == true)
                {
                    bld.AddFeature("USE_DEPTH_CULL");

                    bld.ExecuteAction((ctx, up) =>
                    {
                        up.LoadBuffer(ctx.DepthCullProvider!.DepthCullBuffer, 0);
                    });
                }

                if (bld.Context.UseSharedSsbo)
                    bld.AddFeature("USE_MATERIAL_SSBO");

                bld.AddFeature("USE_CAMERA_POS");

                if (hasPunctual)
                    bld.AddFeature("USE_PUNCTUAL");

                if (imgLight != null)
                    bld.AddFeature("USE_IBL");

                if (DepthNoiseFactor > 0)
                    bld.AddFeature("USE_DEPTH_NOISE");

                if (bld.Context.BloomProvider != null)
                    bld.AddFeature("USE_BLOOM");

                bld.AddFeature("MAX_LIGHTS " + LightListUniforms.Max);

                var envDepth = bld.Context.MainCamera?.Feature<IEnvDepthProvider>();

                if (envDepth != null)
                {
                    bld.AddFeature("HAS_ENV_DEPTH");

                    bld.LoadTexture(() => envDepth.Acquire(_depthCamera), TextureSlots.EnvDepth);

                    bld.ExecuteAction((ctx, up) =>
                    {
                        up.SetUniform("envDepthBias", envDepth.Bias);

                        if (_depthCamera.Eyes != null)
                        {
                            up.SetUniform("envViewProj[0]", _depthCamera.Eyes[0].ViewProj);
                            up.SetUniform("envViewProj[1]", _depthCamera.Eyes[1].ViewProj);
                        }
                    });
                }

                bld.LoadBuffer((ctx) =>
                {
                    var curVer = (bld.Context.Lights?
                            .Where(a => a is not ImageLight)
                            .Sum(a => a.Version + a.ContentVersion) ?? -1);

                    if (ctx.CurrentBuffer == null || ctx.CurrentBuffer.Version == curVer)
                        return null;

                    ctx.CurrentBuffer!.Version = curVer;

                    var result = new LightListUniforms();

                    var count = 0;

                    foreach (var light in bld.Context.Lights!)
                    {
                        if (light is PointLight point)
                        {
                            result.Lights[count] = new LightUniforms
                            {
                                Type = 0,
                                Color = ((Vector3)point.Color) * point.Intensity,
                                Position = point.WorldPosition,
                                Range = point.Range
                            };

                            count++;
                        }
                        else if (light is DirectionalLight directional)
                        {
                            result.Lights[count] = new LightUniforms
                            {
                                Type = 1,
                                Color = ((Vector3)directional.Color) * directional.Intensity,
                                Direction = Vector3.Normalize(directional.Direction)
                            };

                            count++;
                        }
                        else if (light is SpotLight spot)
                        {
                            result.Lights[count] = new LightUniforms
                            {
                                Type = 2,
                                Range = spot.Range,
                                Color = ((Vector3)spot.Color) * spot.Intensity,
                                Direction = Vector3.Normalize(spot.Forward),
                                InConeCos = MathF.Cos(spot.InnerConeAngle),
                                OutConeCos = MathF.Cos(spot.OuterConeAngle),
                                Position = spot.WorldPosition,
                            };
                            count++;
                        }

                        if (count >= LightListUniforms.Max)
                            throw new InvalidOperationException("Max lights reached");
                    }

                    result.Count = (uint)count;

                    return (LightListUniforms?)result;

                }, UniformsSlots.Lights, BufferStore.Shader);

                if (imgLight != null)
                {
                    var hasTransform = ForceIblTransform || imgLight.RotationY != 0 || imgLight.LightTransform != Matrix3x3.Identity;

                    if (hasTransform)
                        bld.AddFeature("USE_IBL_TRANSFORM");

                    bld.LoadBuffer(ctx =>
                    {
                        var version = imgLight.Version + imgLight.ContentVersion;

                        if (ctx.CurrentBuffer == null || version == ctx.CurrentBuffer.Version)
                            return null;

                        ctx.CurrentBuffer!.Version = version;

                        var transform = (imgLight.LightTransform * Matrix3x3.CreateRotationY(imgLight.RotationY)).ToVector4x3();

                        return (IblUniforms?)new IblUniforms
                        {
                            SpecularTextureLevels = imgLight.Textures.MipCount,
                            Intensity = imgLight.Intensity,
                            Color = imgLight.Color.ToVector3(),
                            ShadowStrength = imgLight.ShadowStrength,
                            Transform = transform
                        };
                    }, UniformsSlots.Ibl, BufferStore.Shader);

                    bld.LoadTexture(() => imgLight.Textures.GGXEnv, TextureSlots.IblGgxEnv);
                    bld.LoadTexture(() => imgLight.Textures.LambertianEnv, TextureSlots.IblLambertianEnv);
                    bld.LoadTexture(() => imgLight.Textures.GGXLUT, TextureSlots.IblGgxLut);
                }

                if (UseLightField)
                {
                    if (_lightFieldProvider == null)
                        Context.TryRequire(out _lightFieldProvider);

                    if (_lightFieldProvider != null)
                    {
                        var lightField = _lightFieldProvider.GetLightField();

                        if (lightField.UseAllFaces)
                            bld.AddFeature("USE_LIGHT_FIELD_ALL_FACES");

                        bld.ExecuteAction((ctx, up) =>
                        {
                            lightField = _lightFieldProvider.GetLightField();

                            if (lightField.Textures == null || lightField.Textures.Count == 0)
                                return;

                            var i = 0;

                            foreach (var tex in lightField.Textures)
                            {
                                up.LoadTexture(tex, i + 10);
                                //up.SetUniform($"uLightField[{i}]", i + 10);
                                i++;
                            }

                            up.SetUniform("uLightFieldOrigin", lightField.Origin);
                            up.SetUniform("uLightFieldSize", lightField.Size);
                            up.SetUniform("uVoxelSize", lightField.VoxelSize);
                            up.SetUniform("uLightFieldDifStrength", lightField.DiffuseStrength);
                            up.SetUniform("uLightFieldSpecStrength", lightField.SpecularStrength);
                        });
                    }
                }

                base.UpdateShaderGlobal(bld);
            }

            public bool UseDepthCulling { get; set; }

            public float DepthNoiseFactor { get; set; }

            public float DepthNoiseDistance { get; set; }

            public ToneMapMode ToneMap { get; set; }

            public bool UseLightField { get; set; }
        }

        #endregion

        public static readonly PbrShader SHADER;

        static PbrMaterial()
        {
            SHADER = new PbrShader
            {
                FragmentSourceName = "Pbr/pbr.frag",
                VertexSourceName = "Pbr/pbr.vert",
                TessControlSourceName = "Shared/height_map.tesc",
                TessEvalSourceName = "Shared/height_map.tese",
                GeometrySourceName = "Shared/height_map.geom",
                Resolver = str => Embedded.TryGetString(str),
                VaryByModel = true,
                IsLit = true,
            };
        }

        public PbrMaterial()
        {
            Shader = SHADER;
            Color = Color.White;
            Roughness = 1.0f;
            Metalness = 1.0f;
            OcclusionStrength = 1.0f;
            NormalScale = 1;
            UseInstanceDraw = true;
            ForceIblTransform = false;
            LightFieldOfs = 1.5f;
            UseLightField = UseLightFieldMode.Full;
            AlphaSpecularScale = 0.5f;
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            PlanarReflection? planar = null;

            bld.SetFsIncludes("pbr_defaults.glsl");
            bld.SetFragmentLoader("frag = LoadFragmentProperties();");

            bld.AddFeature($"DEBUG {(int)Debug}");

            if (UseInstanceDraw && bld.Context.UseInstanceDraw)
                bld.AddFeature("USE_INSTANCE");

            if (Simplified)
                bld.AddFeature("SIMPLIFIED");

            if (UseEnvDepth)
                bld.AddFeature("USE_ENV_DEPTH");

            if (ReceiveShadows)
                bld.AddFeature("RECEIVE_SHADOWS");

            if (Color.A == 0)
                bld.AddFeature("TRANSPARENT");

            if (DoubleSided)
                bld.AddFeature("DOUBLE_SIDED");


            bld.LoadBuffer<MaterialUniforms>(ctx =>
            {
                var curVersion = ContentVersion + Version;

                if (ctx.CurrentBuffer == null || curVersion == ctx.CurrentBuffer.Version)
                    return null;

                ctx.CurrentBuffer.Version = curVersion;

                return new MaterialUniforms
                {
                    Color = Color,
                    Metalness = Metalness,
                    Roughness = Roughness,
                    ShadowColor = ShadowColor,
                    OcclusionStrength = OcclusionStrength,
                    NormalScale = NormalScale,
                    AlphaCutoff = AlphaCutoff,
                    AlphaSpecularScale = AlphaSpecularScale,
                    EmissiveColor = EmissiveColor,
                    TexTransform = (ColorMap?.Transform ?? UV0Transform ?? Matrix3x3.Identity).ToVector4x3(),
                    PlanarReflectionStrength = planar?.Strength ?? 0,
                    PlanarReflectionLevel = planar?.BlurLevel ?? 0,
                    Transmission = Transmission,
                };

            },
            UniformsSlots.Material,
            BufferStore.Material,
            bld.Context.UseSharedSsbo ? BufferUsage.SharedSsbo : BufferUsage.Uniforms,
            "uMaterialIndex");

            if (EmissiveColor != Color.Transparent)
                bld.AddFeature("USE_EMISSIVE");

            if (_hosts.Count == 1)
            {
                planar = _hosts.First().Components<PlanarReflection>().FirstOrDefault();

                if (planar != null && planar.IsEnabled)
                {
                    bld.AddFeature("PLANAR_REFLECTION");

                    if (PlanarReflection.IsMultiView)
                        bld.AddFeature("PLANAR_REFLECTION_MV");

                    bld.LoadTexture(() => planar.Texture, TextureSlots.PlanarReflection);

                    bld.ExecuteAction((ctx, up) =>
                    {
                        if (PlanarReflection.IsMultiView)
                        {
                            if (planar.ReflectionCamera.Eyes != null)
                            {
                                up.SetUniform("uReflectMatrix[0]", planar.ReflectionCamera.Eyes[0].ViewProj);
                                up.SetUniform("uReflectMatrix[1]", planar.ReflectionCamera.Eyes[1].ViewProj);
                            }
                        }
                        else
                            up.SetUniform("uReflectMatrix", planar.ReflectionCamera.ViewProjection);
                    });
                }

            }

            if (Color.IsSrgb)
                bld.AddFeature("COLOR_IS_SRGB");

            if (ColorMapProjection != null)
            {
                bld.AddFeature("HAS_COLORMAP_PROJ");
                bld.ExecuteAction((ctx, up) =>
                {
                    up.SetUniform("uColorMapProj", ColorMapProjection.Value);
                });
            }

            if (ClipVolume != null)
            {
                bld.AddFeature("HAS_CLIP_VOLUME");
                bld.ExecuteAction((ctx, up) =>
                {
                    up.SetUniform("uClipMin", ClipVolume.Value.Min);
                    up.SetUniform("uClipMax", ClipVolume.Value.Max);
                });
            }

            if (HeightMap?.Texture != null)
            {
                bld.AddFeature("USE_HEIGHT_MAP");

                if (HeightMap.NormalMode == HeightNormalMode.Sobel)
                    bld.AddFeature("NORMAL_SOBEL");

                else if (HeightMap.NormalMode == HeightNormalMode.Geometry)
                    bld.AddFeature("NORMAL_GEO");

                if (HeightMap.MaskValue != null)
                    bld.AddFeature($"HEIGHT_MASK_VALUE {HeightMap.MaskValue}.0");

                if (HeightMap.SphereRadius > 0)
                {
                    bld.AddFeature("IS_SPHERE");
                    bld.ExecuteAction((ctx, up) =>
                    {
                        up.SetUniform("uSphereRadius", HeightMap.SphereRadius);
                        up.SetUniform("uSphereCenter", HeightMap.SphereWorldCenter);
                    });
                }

                bld.LoadTexture(() => HeightMap?.Texture, TextureSlots.HeightMap);

                bld.ExecuteAction((ctx, up) =>
                {
                    if (HeightMap != null)
                        up.SetUniform("uHeightTexSize", new Vector2(HeightMap.Texture.Width, HeightMap.Texture.Height));

                    up.SetUniform("uHeightNormalStrength", HeightMap!.NormalStrength);
                    up.SetUniform("uHeightScale", HeightMap.ScaleFactor);
                    up.SetUniform("uTargetTriSize", HeightMap.TargetTriSize);
                });
            }

            var uv0Transform = ColorMap?.Transform ?? UV0Transform;

            if (uv0Transform != null)
                bld.AddFeature("HAS_TEX_TRANSFORM uMaterial.texTransform");

            if (ColorMap != null)
            {
                bld.AddFeature("USE_ALBEDO_MAP");

                bld.PrepareTexture(ColorMap);

                bld.LoadTexture(() => ColorMap, TextureSlots.Albedo);

                bld.AddFeature($"ALBEDO_UV_SET {ColorMapUVSet}");
            }

            if (MetallicRoughnessMap != null)
            {
                bld.AddFeature("USE_METALROUGHNESS_MAP");
                bld.LoadTexture(() => MetallicRoughnessMap, TextureSlots.MetallicRoughness);
            }

            else if (SpecularMap != null)
            {
                bld.AddFeature("USE_SPECULAR_MAP");
                bld.LoadTexture(() => SpecularMap, TextureSlots.MetallicRoughness);
            }

            if (NormalMap != null && NormalScale != 0)
            {
                bld.AddFeature("USE_NORMAL_MAP");

                if (NormalMapFormat == NormalMapFormat.UnityBc3)
                    bld.AddFeature("NORMAL_MAP_BC3");

                bld.LoadTexture(() => NormalMap, TextureSlots.Normal);
            }

            if (OcclusionMap != null)
            {
                bld.AddFeature("USE_OCCLUSION_MAP");
                bld.LoadTexture(() => OcclusionMap, TextureSlots.Occlusion);
            }

            if (EmissiveMap != null)
            {
                bld.AddFeature("USE_EMISSIVE_MAP");
                bld.LoadTexture(() => EmissiveMap, TextureSlots.Emissive);
            }

            if (UseLightField != UseLightFieldMode.None && ((PbrShader)_shader!).UseLightField)
            {
                bld.AddFeature("USE_LIGHT_FIELD");

                bld.AddFeature(UseLightField switch
                {
                    UseLightFieldMode.Full => "LIGHT_FIELD_FULL",
                    UseLightFieldMode.Self => "LIGHT_FIELD_SELF",
                    UseLightFieldMode.SelfOmni => "LIGHT_FIELD_SELF_OMNI",
                    _ => throw new NotSupportedException()
                });

                bld.SetUniform("uLightFieldOfs", ctx => LightFieldOfs);
            }

            if (HasIridescence)
            {
                bld.AddFeature("USE_IRIDESCENCE");

                if (IridescenceThicknessMap != null)
                    bld.AddFeature("USE_IRIDESCENCE_THICKNESS_MAP");

                if (IridescenceMap != null)
                    bld.AddFeature("USE_IRIDESCENCE_MAP");

                bld.LoadTexture(() => IridescenceMap, TextureSlots.IridescenceMap);

                bld.LoadTexture(() => IridescenceThicknessMap, TextureSlots.IridescenceThicknessMap);

                bld.LoadBuffer(ctx =>
                {
                    var curVer = _contentVersion + _version;

                    if (ctx.CurrentBuffer == null || curVer == ctx.CurrentBuffer.Version)
                        return null;

                    ctx.CurrentBuffer!.Version = curVer;

                    return (IridescenceUniforms?)new IridescenceUniforms
                    {
                        Factor = IridescenceFactor,
                        Ior = IridescenceIor,
                        ThicknessMaximum = IridescenceThicknessMax,
                        ThicknessMinimum = IridescenceThicknessMin

                    };
                }, UniformsSlots.Iridescence, BufferStore.Material);
            }

            if (Transmission > 0)
            {
                bld.AddFeature("USE_TRANSMISSION");

                bld.AddFeature($"TRANSMISSION_MODE {(int)TransmissionMode}");

                if (TransmissionMap != null)
                    bld.AddFeature("USE_TRANSMISSION_MAP");

                if (TransmissionMode == TransmissionMode.FrameBufferFetch)
                {
                    Alpha = AlphaMode.Opaque;
                }
                else if (TransmissionMode == TransmissionMode.DualAlpha)
                {
                    Alpha = AlphaMode.TransmissionBlend;
#if GLES
                    bld.AddExtension("GL_EXT_blend_func_extended");
#endif
                }
                else if (TransmissionMode == TransmissionMode.Texture)
                {
                    if (!HasRefraction)
                        bld.LoadTexture(ctx => ctx.TransmissionForeground, TextureSlots.VolumeForeground);
                }

                bld.LoadTexture(() => TransmissionMap, TextureSlots.TransmissionMap);
            }

            if (HasRefraction)
            {
                bld.AddFeature("USE_REFRACTION");

                var refSrc = bld.Context.Scene?.Feature<IScreenRefractionSource>();
                if (refSrc != null)
                {

                    bld.AddFeature("VOLUME_BACKGROUND");

                    bld.LoadTexture(ctx=> 
                        refSrc.GetRefractionTextures((PerspectiveCamera)ctx.PassCamera!)[0], TextureSlots.VolumeBackground);
                }

                if (ThicknessMap != null)
                    bld.AddFeature("USE_THICKNESS_MAP");

                bld.LoadTexture(ctx => ctx.TransmissionForeground, TextureSlots.VolumeForeground);

                bld.LoadTexture(() => ThicknessMap, TextureSlots.ThicknessMap);

                bld.LoadBuffer(ctx =>
                {
                    var curVer = _contentVersion + _version;

                    if (ctx.CurrentBuffer == null || curVer == ctx.CurrentBuffer.Version)
                        return null;

                    ctx.CurrentBuffer!.Version = curVer;

                    return (VolumeUniforms?)new VolumeUniforms
                    {
                        AttenuationColor = AttenuationColor.ToVector3(),
                        AttenuationDistance = AttenuationDistance == 0 ? float.PositiveInfinity : AttenuationDistance,
                        Ior = Ior,
                        Thickness = Thickness,
                    };
                }, UniformsSlots.Volume, BufferStore.Material);
            }


            bld.AddFeature($"ALPHA_MODE {(int)(Alpha == AlphaMode.BlendMain ? AlphaMode.Blend : Alpha)}");

            if (AlphaSpecularScale > 0)
                bld.AddFeature("USE_ALPHA_SPECULAR");

            if ((bld.Context.ActiveComponents & VertexComponent.Tangent) != 0)
                bld.AddFeature("HAS_TANGENTS");

            if ((bld.Context.ActiveComponents & VertexComponent.UV1) != 0)
                bld.AddFeature("HAS_UV2");

            base.UpdateShaderMaterial(bld);
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);

            container.Write(nameof(Color), Color);
            container.Write(nameof(ShadowColor), ShadowColor);
            container.Write(nameof(Metalness), Metalness);
            container.Write(nameof(Roughness), Roughness);
            container.Write(nameof(OcclusionStrength), OcclusionStrength);
            container.Write(nameof(AlphaCutoff), AlphaCutoff);
            container.Write(nameof(NormalScale), NormalScale);
            container.Write(nameof(UseEnvDepth), UseEnvDepth);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            UseEnvDepth = container.Read<bool>(nameof(UseEnvDepth));
            NormalScale = container.Read<float>(nameof(NormalScale));
            AlphaCutoff = container.Read<float>(nameof(AlphaCutoff));
            OcclusionStrength = container.Read<float>(nameof(OcclusionStrength));
            Roughness = container.Read<float>(nameof(Roughness));
            Metalness = container.Read<float>(nameof(Metalness));
            ShadowColor = container.Read<Color>(nameof(ShadowColor));
            Color = container.Read<Color>(nameof(Color));

            base.SetStateWork(container);
        }

        TessellationMode ITessellationMaterial.TessellationMode =>
            HeightMap?.Texture != null ? (HeightMap.NormalMode == HeightNormalMode.Geometry ?
                                            TessellationMode.Geometry :
                                            TessellationMode.Normal)
                                        : TessellationMode.None;

        bool ITessellationMaterial.DebugTessellation => HeightMap?.DebugTessellation ?? false;

        [Category(Rendering)]
        public HeightMapSettings? HeightMap { get; set; }

        [Category(Rendering)]
        public Bounds3? ClipVolume { get; set; }

        [Category(Textures)]
        public Texture2D? OcclusionMap { get; set; }

        [Category(Textures)]
        public Texture2D? ColorMap { get; set; }

        [Category(Textures)]
        public uint ColorMapUVSet { get; set; }

        [Category(Textures)]
        public Texture2D? MetallicRoughnessMap { get; set; }

        [Category(Textures)]
        public Texture2D? SpecularMap { get; set; }

        [Category(Textures)]
        public Texture2D? NormalMap { get; set; }

        [Category(Textures)]
        public Texture2D? EmissiveMap { get; set; }

        [Category(Textures)]
        public NormalMapFormat NormalMapFormat { get; set; }

        [Category(Surface)]
        public bool ReceiveShadows { get; set; }

        [Category(Surface)]
        public Color ShadowColor { get; set; }

        [Category(Surface)]
        public Color Color { get; set; }

        [Category(Surface)]
        [Range(0, 1, 0.01f)]
        public float Metalness { get; set; }

        [Category(Surface)]
        [Range(0, 1, 0.01f)]
        public float Roughness { get; set; }

        [Category(Surface)]
        [Range(0, 1, 0.01f)]
        public float OcclusionStrength { get; set; }

        [Category(Rendering)]
        public float AlphaCutoff { get; set; }

        [Category(Rendering)]
        [Range(0, 2, 0.01f)]
        public float AlphaSpecularScale { get; set; }

        [Category(Surface)]
        public float NormalScale { get; set; }

        [Category(Rendering)]
        public bool UseEnvDepth { get; set; }

        [Category(Surface)]
        public Color EmissiveColor { get; set; }

        [Category(Rendering)]
        public bool Simplified { get; set; }

        [Category(Rendering)]
        public PbrDebug Debug { get; set; }

        [Category(Rendering)]
        public float LightFieldOfs { get; set; }

        [Category(Rendering)]
        public UseLightFieldMode UseLightField { get; set; }

        [Category(Rendering)]
        public bool UseInstanceDraw { get; set; }

        [Category(Textures)]
        public Matrix3x3? UV0Transform { get; set; }

        [Category(Textures)]
        public Matrix4x4? ColorMapProjection { get; set; }


        [Category(Surface)]
        [Range(0, 1, 0.01f)]
        public float Transmission { get; set; }

        [Category(Surface)]
        public TransmissionMode TransmissionMode { get; set; }

        [Category(Textures)]
        public Texture2D? TransmissionMap { get; set; }

        [Category(Volume)]
        public float Ior { get; set; }

        [Category(Volume)]
        [Range(0, 0.1f, 0.001f)]
        public float Thickness { get; set; }

        [Category(Volume)]
        [Range(0, 1, 0.01f)]
        public float AttenuationDistance { get; set; }

        [Category(Volume)]
        public Color AttenuationColor { get; set; }

        [Category(Volume)]
        public Texture2D? ThicknessMap { get; set; }

        [Category(Iridescence)]
        public float IridescenceFactor { get; set; }

        [Category(Iridescence)]
        public float IridescenceIor { get; set; }

        [Category(Iridescence)]
        public float IridescenceThicknessMin { get; set; }

        [Category(Iridescence)]
        public float IridescenceThicknessMax { get; set; }

        [Category(Iridescence)]
        public Texture2D? IridescenceThicknessMap { get; set; }

        [Category(Iridescence)]
        public Texture2D? IridescenceMap { get; set; }

        public bool HasTransmission => Transmission > 0 || Thickness > 0;

        public bool HasRefraction => Thickness > 0 && Ior != 1;

        public bool HasIridescence => IridescenceFactor > 0;

        public static bool ForceIblTransform { get; set; }

    }
}
