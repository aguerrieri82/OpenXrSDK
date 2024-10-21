using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using XrMath;

namespace XrEngine
{
    public class PbrV2Material : ShaderMaterial, IColorSource, IShadowMaterial, IPbrMaterial, IEnvDepthMaterial
    {
        #region CameraUniforms

        [StructLayout(LayoutKind.Explicit, Size = 160)]
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

            [FieldOffset(148)]
            public Size2I ViewSize;
        }

        #endregion

        #region MaterialUniforms

        [StructLayout(LayoutKind.Explicit, Size = 128)]
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

        #region GlobalShaderHandler

        class GlobalShaderHandler : IShaderHandler
        {
            long _iblVersion = -1;
            PerspectiveCamera _depthCamera = new PerspectiveCamera();   

            public bool NeedUpdateShader(UpdateShaderContext ctx)
            {
                var ibl = ctx.Lights?.OfType<ImageLight>().FirstOrDefault();
                return ctx.LastUpdate?.LightsHash != ctx.LightsHash || 
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

                if (bld.Context.ShadowMapProvider != null)
                {
                    var mode = bld.Context.ShadowMapProvider.Options.Mode;

                    if (mode != ShadowMapMode.None)
                    {
                        bld.AddFeature("USE_SHADOW_MAP");
                        bld.AddFeature("SHADOW_MAP_MODE " + (int)mode);

                        bld.ExecuteAction((ctx, up) =>
                        {
                            up.SetUniform("uShadowMap", ctx.ShadowMapProvider!.ShadowMap!, 14);
                        });
                    }
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
                            up.SetUniform("envDepth", texture, 8);

                        up.SetUniform("envDepthBias", envDepth.Bias);

                        if (_depthCamera.Eyes != null)
                        {
                            up.SetUniform("envViewProj[0]", _depthCamera.Eyes[0].ViewProj);
                            up.SetUniform("envViewProj[1]", _depthCamera.Eyes[1].ViewProj);
                        }
                    });
                }


                bld.SetUniformBuffer("Camera", (ctx) =>
                {
                    var result = new CameraUniforms
                    {
                        ViewProj = ctx.Camera!.ViewProjection,
                        Position = ctx.Camera!.WorldPosition,
                        Exposure = ctx.Camera.Exposure,
                        //EnvDepthBias = envDepth?.Bias ?? 0,
                        ActiveEye = ctx.Camera.ActiveEye,   
                        ViewSize = ctx.ViewSize  
                    };

                    var light = ctx.ShadowMapProvider?.LightCamera?.ViewProjection;
                    if (light != null)
                        result.LightSpaceMatrix = light.Value;

                    return (CameraUniforms?)result;

                }, 0, true);


                bld.SetUniformBuffer("Lights", (ctx) =>
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
                }, 1, true);


                if (imgLight != null)
                {
                    if (imgLight.Rotation != 0)
                        bld.AddFeature("USE_IBL_TRANSFORM");

                    bld.ExecuteAction((ctx, up) =>
                    {
                        up.SetUniform("uSpecularTextureLevels", (float)imgLight.Textures.MipCount);
                        up.SetUniform("uIblIntensity", (float)imgLight.Intensity);

                        if (imgLight.Rotation > 0)
                            up.SetUniform("uIblTransform", Matrix3x3.CreateRotationY(imgLight.Rotation));

                        if (imgLight.Textures?.GGXEnv != null)
                            up.SetUniform("specularTexture", imgLight.Textures.GGXEnv, 4);

                        if (imgLight.Textures?.LambertianEnv != null)
                            up.SetUniform("irradianceTexture", imgLight.Textures.LambertianEnv, 5);

                        if (imgLight.Textures?.GGXLUT != null)
                            up.SetUniform("specularBRDF_LUT", imgLight.Textures.GGXLUT, 6);

                    });
                }

            }

           
        }

        #endregion

        static readonly Shader SHADER;

        static PbrV2Material()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "PbrV2/pbr_fs.glsl",
                VertexSourceName = "PbrV2/pbr_vs.glsl",
                Resolver = str => Embedded.GetString(str),
                IsLit = true,
                UpdateHandler = new GlobalShaderHandler()
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

            bld.SetUniformBuffer("Material", ctx => (MaterialUniforms?)material, 2, false);


            var planar = bld.Context.Model!.Components<PlanarReflection>().FirstOrDefault();

            if (planar != null)
            {
                bld.AddFeature("PLANAR_REFLECTION");
                
                if (PlanarReflection.IsMultiView)
                    bld.AddFeature("PLANAR_REFLECTION_MV");

                bld.ExecuteAction((ctx, up) =>
                {
                    up.SetUniform("reflectionTexture", planar.Texture, 7);

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
                bld.SetUniform("albedoTexture", ctx => ColorMap, 0);

                if (ColorMap.Transform != null)
                {
                    bld.AddFeature("HAS_TEX_TRANSFORM");
                    material.TexTransform = ColorMap.Transform.Value;
                }
            }

            if (MetallicRoughnessMap != null)
            {
                bld.AddFeature("USE_METALROUGHNESS_MAP");
                bld.SetUniform("metalroughnessTexture", ctx => MetallicRoughnessMap, 2);
            }

            if (NormalMap != null)
            {
                bld.AddFeature("USE_NORMAL_MAP");
                bld.SetUniform("normalTexture", ctx => NormalMap, 1);
            }

            if (OcclusionMap != null)
            {
                bld.AddFeature("USE_OCCLUSION_MAP");
                bld.SetUniform("occlusionTexture", ctx => OcclusionMap, 3);
            }

            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uModel", ctx.Model!.WorldMatrix);
                up.SetUniform("uNormalMatrix", ctx.Model!.NormalMatrix);
            });

            if ((bld.Context.ActiveComponents & VertexComponent.Tangent) != 0)
                bld.AddFeature("HAS_TANGENTS");

            base.UpdateShader(bld);
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
    }
}
