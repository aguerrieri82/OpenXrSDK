using Common.Interop;
using System.Numerics;
using System.Runtime.InteropServices;
using XrMath;

namespace XrEngine
{
    public class PbrV2Material : ShaderMaterial, IColorSource, IShadowMaterial, IPbrMaterial, IEnvDepthMaterial
    {
        const int CAMERA_BUF = 1;
        const int LIGHTS_BUF = 2;
        const int MATERIAL_BUF = 3;

        #region CameraUniforms

        [StructLayout(LayoutKind.Explicit, Size = 180)]
        public struct CameraUniforms
        {
            [FieldOffset(0)]

            public Matrix4x4 ViewProj;

            [FieldOffset(64)]

            public Vector3 Position;

            [FieldOffset(76)]
            public float Exposure;

            [FieldOffset(80)]
            public Matrix4x4 LightSpaceMatrix;

            [FieldOffset(144)]
            public int ActiveEye;

            [FieldOffset(152)]
            public Size2I ViewSize;

            [FieldOffset(160)]
            public float NearPlane;

            [FieldOffset(164)]
            public float FarPlane;

            [FieldOffset(168)]
            public float DepthNoiseFactor;

            [FieldOffset(172)]
            public float DepthNoiseDistance;
        }

        #endregion

        #region MaterialUniforms

        [StructLayout(LayoutKind.Explicit, Size = 256)]
        public struct MaterialUniforms
        {
            [FieldOffset(0)]

            public Vector4 Color;

            [FieldOffset(16)]
            public float Metalness;

            [FieldOffset(20)]
            public float Roughness;

            [FieldOffset(32)]
            public Matrix3x3Aligned TexTransform;

            [FieldOffset(80)]
            public float OcclusionStrength;

            [FieldOffset(96)]
            public Color ShadowColor;

            [FieldOffset(112)]
            public float NormalScale;

            [FieldOffset(116)]
            public float AlphaCutoff;


        }

        #endregion

        #region LightListUniforms

        [StructLayout(LayoutKind.Explicit)]
        public struct LightListUniforms : IDynamicBuffer
        {
            public static int Max = 3;
            static DynamicBuffer _buffer;

            [FieldOffset(0)]
            public uint Count;
            [FieldOffset(16)]
            public LightUniforms[] Lights;

            public unsafe DynamicBuffer GetBuffer()
            {
                var newSize = (sizeof(LightUniforms) * Max) + 16;

                if (_buffer.Size != newSize)
                {
                    if (_buffer.Data != 0)
                        MemoryManager.Free(_buffer.Data);

                    _buffer.Size = (uint)newSize;
                    _buffer.Data = MemoryManager.Allocate((int)_buffer.Size, this);
                }

                ((int*)_buffer.Data)[0] = Lights.Length;

                fixed (LightUniforms* pArray = Lights)
                {
                    var srcSpan = new Span<LightUniforms>(pArray, Lights.Length);
                    var dstSpan = new Span<LightUniforms>((LightUniforms*)(_buffer.Data + 16), Lights.Length);
                    srcSpan.CopyTo(dstSpan);
                }

                //Log.Debug(this, "Update light buffer");

                return _buffer;
            }
        }

        #endregion

        #region LightUniforms

        [StructLayout(LayoutKind.Explicit, Size = 64)]
        public struct LightUniforms
        {
            [FieldOffset(0)]
            public uint Type;

            [FieldOffset(16)]
            public Vector3 Position;

            [FieldOffset(32)]
            public Vector3 Direction;

            [FieldOffset(48)]
            public Vector3 Color;

            [FieldOffset(60)]
            public float Range;
        }

        #endregion

        #region ModelUniforms

        [StructLayout(LayoutKind.Explicit, Size = 128)]
        public struct ModelUniforms
        {
            [FieldOffset(0)]
            public Matrix4x4 WorldMatrix;

            [FieldOffset(64)]
            public Matrix4x4 NormalMatrix;
        }

        #endregion

        #region PbrV2Shader

        public class PbrV2Shader : Shader, IShaderHandler
        {
            long _iblVersion = -1;
            readonly PerspectiveCamera _depthCamera = new PerspectiveCamera();

            public bool NeedUpdateShader(UpdateShaderContext ctx)
            {
                var ibl = ctx.Lights?.OfType<ImageLight>().FirstOrDefault();
                return ctx.LastGlobalUpdate?.LightsHash != ctx.LightsHash ||
                       (ctx.LastGlobalUpdate?.ShaderVersion != Version) ||
                       (ibl?.Version ?? -1) != _iblVersion;
            }

            public void UpdateShader(ShaderUpdateBuilder bld)
            {
                var imgLight = bld.Context.Lights?.OfType<ImageLight>().FirstOrDefault();

                var hasPunctual = bld.Context.Lights!.Any(a => a != imgLight);

                if (hasPunctual)
                    bld.AddFeature("USE_PUNCTUAL");

                if (imgLight != null)
                {
                    _iblVersion = imgLight.Version;
                    bld.AddFeature("USE_IBL");
                }

                if (DepthNoiseFactor > 0)
                    bld.AddFeature("USE_DEPTH_NOISE");

                if (bld.Context.ShadowMapProvider != null)
                {
                    var mode = bld.Context.ShadowMapProvider.Options.Mode;

                    if (mode != ShadowMapMode.None)
                    {
                        bld.AddFeature("USE_SHADOW_MAP");
                        bld.AddFeature("SHADOW_MAP_MODE " + (int)mode);

                        bld.ExecuteAction((ctx, up) =>
                        {
                            up.LoadTexture(ctx.ShadowMapProvider!.ShadowMap!, 14);
                        });
                    }
                }

                if (bld.Context.BloomProvider != null)
                {
                    bld.AddFeature("USE_BLOOM");
                }


                bld.AddFeature("MAX_LIGHTS " + LightListUniforms.Max);

                var envDepth = bld.Context.Camera?.Feature<IEnvDepthProvider>();

                if (envDepth != null)
                {
                    bld.AddFeature("HAS_ENV_DEPTH");
                    bld.ExecuteAction((ctx, up) =>
                    {
                        var texture = envDepth.Acquire(_depthCamera);
                        if (texture != null)
                            up.LoadTexture(texture, 8);

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
                    var result = new CameraUniforms
                    {
                        ViewProj = ctx.Camera!.ViewProjection,
                        Position = ctx.Camera!.WorldPosition,
                        Exposure = ctx.Camera.Exposure,
                        ActiveEye = ctx.Camera.ActiveEye,
                        ViewSize = ctx.Camera.ViewSize,
                        NearPlane = ctx.Camera.Near,
                        FarPlane = ctx.Camera.Far,
                        DepthNoiseFactor = DepthNoiseFactor,
                        DepthNoiseDistance = DepthNoiseDistance
                    };

                    var light = ctx.ShadowMapProvider?.LightCamera?.ViewProjection;
                    if (light != null)
                        result.LightSpaceMatrix = light.Value;

                    return (CameraUniforms?)result;

                }, 0, BufferStore.Shader);


                bld.LoadBuffer((ctx) =>
                {
                    var hash = bld.Context.Lights!.Sum(a => a.Version).ToString();

                    if (ctx.CurrentBuffer!.Hash == hash)
                        return null;

                    ctx.CurrentBuffer!.Hash = hash;

                    Log.Debug(this, "Build light uniforms");

                    if (!hasPunctual)
                    {
                        return (LightListUniforms?)new LightListUniforms
                        {
                            Count = 0,
                            Lights = []
                        };
                    }

                    var lights = new List<LightUniforms>();

                    foreach (var light in bld.Context.Lights!)
                    {
                        if (light is PointLight point)
                        {
                            lights.Add(new LightUniforms
                            {
                                Type = 0,
                                Color = ((Vector3)point.Color) * point.Intensity,
                                Position = point.WorldPosition,
                                Range = point.Range
                            });
                        }
                        else if (light is DirectionalLight directional)
                        {
                            lights.Add(new LightUniforms
                            {
                                Type = 1,
                                Color = ((Vector3)directional.Color) * directional.Intensity,
                                Direction = Vector3.Normalize(directional.Direction)

                            });
                        }
                    }

                    return (LightListUniforms?)new LightListUniforms
                    {
                        Count = (uint)lights.Count,
                        Lights = lights.ToArray()
                    };
                }, 1, BufferStore.Shader);


                if (imgLight != null)
                {
                    var hasTransform = ForceIblTransform || imgLight.RotationY != 0 || imgLight.LightTransform != Matrix3x3.Identity;

                    if (hasTransform)
                        bld.AddFeature("USE_IBL_TRANSFORM");

                    bld.ExecuteAction((ctx, up) =>
                    {
                        up.SetUniform("uSpecularTextureLevels", (float)imgLight.Textures.MipCount);
                        up.SetUniform("uIblIntensity", (float)imgLight.Intensity);
                        up.SetUniform("uIblColor", new Vector3(imgLight.Color.R, imgLight.Color.G, imgLight.Color.B));

                        if (hasTransform)
                            up.SetUniform("uIblTransform", imgLight.LightTransform * Matrix3x3.CreateRotationY(imgLight.RotationY));

                        if (imgLight.Textures?.GGXEnv != null)
                            up.LoadTexture(imgLight.Textures.GGXEnv, 4);

                        if (imgLight.Textures?.LambertianEnv != null)
                            up.LoadTexture(imgLight.Textures.LambertianEnv, 5);

                        if (imgLight.Textures?.GGXLUT != null)
                            up.LoadTexture(imgLight.Textures.GGXLUT, 6);

                    });
                }
            }

            public float DepthNoiseFactor { get; set; }

            public float DepthNoiseDistance { get; set; }
        }

        #endregion


        public static readonly PbrV2Shader SHADER;

        static PbrV2Material()
        {
            SHADER = new PbrV2Shader
            {
                FragmentSourceName = "PbrV2/pbr_fs.glsl",
                VertexSourceName = "PbrV2/pbr_vs.glsl",
                Resolver = str => Embedded.GetString(str),
                IsLit = true,
            };
        }

        public PbrV2Material()
        {
            Shader = SHADER;
            Color = Color.White;
            Roughness = 1.0f;
            Metalness = 1.0f;
            OcclusionStrength = 1.0f;
            NormalScale = 1;
            ToneMap = true;
        }


        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            var material = new MaterialUniforms
            {
                Color = Color,
                Metalness = Metalness,
                Roughness = Roughness,
                ShadowColor = ShadowColor,
                OcclusionStrength = OcclusionStrength,
                NormalScale = NormalScale,
                AlphaCutoff = AlphaCutoff,
            };

            if (ToneMap)
                bld.AddFeature("TONEMAP");

            if (UseEnvDepth)
                bld.AddFeature("USE_ENV_DEPTH");

            if (ReceiveShadows)
                bld.AddFeature("RECEIVE_SHADOWS");

            if (Color.A == 0)
                bld.AddFeature("TRANSPARENT");

            if (DoubleSided)
                bld.AddFeature("DOUBLE_SIDED");

            bld.AddFeature($"ALPHA_MODE {(int)(Alpha == AlphaMode.BlendMain ? AlphaMode.Blend : Alpha)}");


            bld.LoadBuffer(ctx =>
            {
                var curVersion = ctx.Model!.Transform.Version;
                if (curVersion == ctx.CurrentBuffer!.Version)
                    return null;
                ctx.CurrentBuffer!.Version = curVersion;

                return (ModelUniforms?)new ModelUniforms
                {
                    NormalMatrix = ctx.Model.NormalMatrix,
                    WorldMatrix = ctx.Model.WorldMatrix
                };
            }, 3, BufferStore.Model);


            bld.LoadBuffer(ctx =>
            {
                var curVersion = Version;
                if (curVersion == ctx.CurrentBuffer!.Version)
                    return null;
                ctx.CurrentBuffer!.Version = curVersion;

                return (MaterialUniforms?)material;

            }, 2, BufferStore.Material);


            var planar = bld.Context.Model!.Components<PlanarReflection>().FirstOrDefault();

            if (planar != null)
            {
                bld.AddFeature("PLANAR_REFLECTION");

                if (PlanarReflection.IsMultiView)
                    bld.AddFeature("PLANAR_REFLECTION_MV");

                bld.ExecuteAction((ctx, up) =>
                {
                    if (planar.Texture != null)
                        up.LoadTexture(planar.Texture, 7);

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


            if (ColorMap != null)
            {
                bld.AddFeature("USE_ALBEDO_MAP");
                bld.LoadTexture(ctx => ColorMap, 0);

                if (ColorMap.Transform != null)
                {
                    bld.AddFeature("HAS_TEX_TRANSFORM");
                    material.TexTransform = ColorMap.Transform.Value;
                }
            }

            if (MetallicRoughnessMap != null)
            {
                bld.AddFeature("USE_METALROUGHNESS_MAP");
                bld.LoadTexture(ctx => MetallicRoughnessMap, 2);
            }

            if (NormalMap != null)
            {
                bld.AddFeature("USE_NORMAL_MAP");
                bld.LoadTexture(ctx => NormalMap, 1);
            }

            if (OcclusionMap != null)
            {
                bld.AddFeature("USE_OCCLUSION_MAP");
                bld.LoadTexture(ctx => OcclusionMap, 3);
            }

            if ((bld.Context.ActiveComponents & VertexComponent.Tangent) != 0)
                bld.AddFeature("HAS_TANGENTS");

            base.UpdateShader(bld);
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


        public Texture2D? OcclusionMap { get; set; }

        public Texture2D? ColorMap { get; set; }

        public Texture2D? MetallicRoughnessMap { get; set; }

        public Texture2D? NormalMap { get; set; }

        public bool ReceiveShadows { get; set; }

        public Color ShadowColor { get; set; }

        public Color Color { get; set; }

        [Range(0, 1, 0.01f)]
        public float Metalness { get; set; }

        [Range(0, 1, 0.01f)]
        public float Roughness { get; set; }


        [Range(0, 1, 0.01f)]
        public float OcclusionStrength { get; set; }

        public bool ToneMap { get; set; }

        //TODO: Implement AlphaCutoff   
        public float AlphaCutoff { get; set; }

        public float NormalScale { get; set; }

        public bool UseEnvDepth { get; set; }

        public static bool ForceIblTransform { get; set; }
    }
}
