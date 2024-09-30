using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using XrMath;

namespace XrEngine
{
    public class PbrV2Material : ShaderMaterial, IColorSource, IShadowMaterial
    {
        [StructLayout(LayoutKind.Explicit, Size = 80)]  
        public struct CameraUniforms
        {
            [FieldOffset(0)]

            public Matrix4x4 ViewProj;

            [FieldOffset(64)]

            public Vector3 Position;
        }


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
        }

        #region GlobalShaderHandler

        class GlobalShaderHandler : IShaderHandler
        {
            public bool NeedUpdateShader(UpdateShaderContext ctx, ShaderUpdate lastUpdate)
            {
                return lastUpdate.LightsHash != ctx.LightsHash;
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
                        ViewProj = ctx.Camera!.ViewProjection,
                        Position = ctx.Camera!.WorldPosition,
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
                                    Type = 0,
                                    Color = (Vector3)point.Color,
                                    Position = point.WorldPosition,
                                });
                            }
                            else if (light is DirectionalLight directional)
                            {
                                lights.Add(new LightUniforms
                                {
                                    Type = 1,
                                    Color = (Vector3)directional.Color,
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
                }

                if (bld.Context.ShadowMapProvider != null)
                {
                    var mode = bld.Context.ShadowMapProvider.Options.Mode;

                    if (mode != ShadowMapMode.None)
                    {
                        bld.AddFeature("USE_SHADOW_MAP");

                        if (mode == ShadowMapMode.HardSmooth)
                            bld.AddFeature("SMOOTH_SHADOW_MAP");

                        bld.ExecuteAction((ctx, up) =>
                        {
                            up.SetUniform("uShadowMap", ctx.ShadowMapProvider!.ShadowMap!, 14);
                            up.SetUniform("uLightSpaceMatrix", ctx.ShadowMapProvider!.LightCamera!.ViewProjection);
                        });
                    }
                }


                if (imgLight != null)
                {

                    bld.ExecuteAction((ctx, up) =>
                    {
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
                IsLit = true
            };
        }

        public PbrV2Material()
        {
            Shader = SHADER;    
        }


        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                if (ColorMap != null)
                    up.SetUniform("albedoTexture", ColorMap, 0);
                
                if (NormalMap != null)
                    up.SetUniform("normalTexture", NormalMap, 1);

                if (MetallicRoughnessMap != null)
                    up.SetUniform("metalroughnessTexture", MetallicRoughnessMap, 2);

                up.SetUniform("uModel", ctx.Model!.WorldMatrix);
            }); 

            base.UpdateShader(bld);
        }

        public Texture2D? ColorMap { get; set; }

        public Texture2D? MetallicRoughnessMap { get; set; }

        public Texture2D? NormalMap { get; set; }

        public bool ReceiveShadows { get; set; }
        
        public Color ShadowColor { get; set; }

        public Color Color { get; set; }


        public static readonly IShaderHandler GlobalHandler = new GlobalShaderHandler();
    }
}
