using System.Diagnostics;
using System.Numerics;
using XrMath;

namespace XrEngine
{

    public class StandardVertexShader : Shader, IShaderHandler, IInstanceShader
    {
        public StandardVertexShader()
        {
            VertexSourceName = "standard.vert";
            Resolver = str => Embedded.GetString(str);
            UseSharedSsbo = true;
        }

        public void UpdateShader(ShaderUpdateBuilder bld)
        {
            var stage = bld.Context.Stage;

            if (stage == UpdateShaderStage.Any || stage == UpdateShaderStage.Model)
                UpdateShaderModel(bld);

            if (stage == UpdateShaderStage.Any || stage == UpdateShaderStage.Shader)
                UpdateShaderGlobal(bld);
        }

        protected virtual void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            bld.LoadBuffer(ctx =>
            {
                //Get the word matrix trigger the update
                var modelWord = ctx.Model!.WorldMatrix;

                var curVersion = ctx.Model!.Transform.Version;

                if (curVersion == ctx.CurrentBuffer!.Version)
                    return null;

                ctx.CurrentBuffer!.Version = curVersion;

                return (ModelUniforms?)new ModelUniforms
                {
                    NormalMatrix = ctx.Model.NormalMatrix,
                    PrevWorldMatrix = ctx.MotionVectorProvider?.GetPrevMatrix(ctx.Model) ?? modelWord,
                    WorldMatrix = modelWord
                };

            }, UniformsSlots.Model, BufferStore.Model,
               UseSharedSsbo ? BufferUsage.SharedSsbo : BufferUsage.Uniforms, "uModelIndex");


            SkinVertexShader.UpdateShaderModel(bld);
        }


        bool IInstanceShader.NeedUpdate(Object3D model, long curVersion)
        {
            //Get the word matrix trigger the update;
            var wordMatrix = model.WorldMatrix;
            return model.Transform.Version != curVersion;
        }

        unsafe long IInstanceShader.Update(UpdateShaderContext ctx, byte* destData, Object3D model, int drawId)
        {
            *(ModelUniforms*)destData = new ModelUniforms
            {
                NormalMatrix = model.NormalMatrix,
                WorldMatrix = model.WorldMatrix,
                PrevWorldMatrix = ctx.MotionVectorProvider?.GetPrevMatrix(model) ?? model.WorldMatrix,
                DrawId = drawId
            };

            return model.Transform.Version;
        }

        protected virtual void UpdateShaderGlobal(ShaderUpdateBuilder bld)
        {
            var shadowOpt = bld.Context.ShadowMapProvider?.Options;

            var shadowMode = shadowOpt?.Mode ?? ShadowMapMode.None;

            if (UseSharedSsbo)
                bld.AddFeature("USE_MODEL_SSBO");

            if (shadowMode != ShadowMapMode.None)
            {
                bld.AddFeature("USE_SHADOW_MAP");
                bld.AddFeature("SHADOW_MAP_MODE " + (int)shadowMode);
                bld.AddFeature("SHADOW_BIAS " + (int)(shadowOpt?.BiasMode ?? ShadowMapBiasMode.None));

                if (shadowOpt!.UseShadowSampler)
                    bld.AddFeature("USE_SHADOW_SAMPLER");

                bld.ExecuteAction((ctx, up) =>
                {
                    if (ctx.ShadowMapProvider?.ShadowMap != null)
                        up.LoadTexture(ctx.ShadowMapProvider!.ShadowMap!, TextureSlots.ShadowMap);

                    if (ctx.ShadowMapProvider?.Light != null)
                        up.SetUniform("uLightDirection", ctx.ShadowMapProvider.Light.Direction);

                    if (shadowOpt?.BiasMode == ShadowMapBiasMode.Value)
                        up.SetUniform("uShadowBias", shadowOpt!.Bias);

                    if (shadowMode == ShadowMapMode.VSM)
                        up.SetUniform("uLightBleed", shadowOpt!.LightBleed);
                });
            }

            bld.LoadBuffer((ctx) =>
            {
                Debug.Assert(ctx.PassCamera != null);

                var result = new CameraUniforms
                {
                    ViewProj = ctx.PassCamera.ViewProjection,
                    Position = ctx.PassCamera.WorldPosition,
                    Exposure = ctx.PassCamera.Exposure,
                    ActiveEye = ctx.PassCamera.ActiveEye,
                    ViewSize = ctx.PassCamera.ViewSize,
                    NearPlane = ctx.PassCamera.Near,
                    FarPlane = ctx.PassCamera.Far,
                    FrustumPlane1 = ctx.FrustumPlanes[0],
                    FrustumPlane2 = ctx.FrustumPlanes[1],
                    FrustumPlane3 = ctx.FrustumPlanes[2],
                    FrustumPlane4 = ctx.FrustumPlanes[3],
                    FrustumPlane5 = ctx.FrustumPlanes[4],
                    FrustumPlane6 = ctx.FrustumPlanes[5],
                    View = ctx.PassCamera.View,
                    Proj = ctx.PassCamera.Projection,
                    ViewProjInv = ctx.PassCamera.ViewProjectionInverse
                };

                var light = ctx.ShadowMapProvider?.LightCamera?.ViewProjection;
                if (light != null)
                    result.LightSpaceMatrix = light.Value;

                return (CameraUniforms?)result;

            }, UniformsSlots.Camera, BufferStore.Shader);

            if (bld.Context.UseMotionVectors && UseMotionVectors)
            {
                bld.AddFeature("MOTION_VECTORS");

                bld.ExecuteAction((ctx, up) =>
                {
                    var texture = ctx.MotionVectorProvider?.Texture;

                    if (texture != null)
                    {
                        var size = new Vector2(texture.Width, texture.Height);
                        var scale = size / ctx.PassCamera!.ViewSize.ToVector2();

                        up.SetUniform("uMotionImageScale", scale);
                        up.LoadImage(texture, ImagesSlots.MotionVectors, BufferAccessMode.Write);
                    }

                    var matrices = ctx.MotionVectorProvider?.GetPrevMatrix(ctx.PassCamera!);

                    if (matrices != null)
                    {
                        up.SetUniform("uPrevViewProj[0]", matrices[0]);

                        if (matrices.Length > 1)
                            up.SetUniform("uPrevViewProj[1]", matrices[1]);
                    }
                });
            }
        }

        public virtual bool NeedUpdateShader(UpdateShaderContext ctx)
        {
            return false;
        }

        public static readonly StandardVertexShader Instance = new();

        public bool UseSharedSsbo { get; set; }

        public bool UseMotionVectors { get; set; }

        public Type InstanceBufferType => typeof(ModelUniforms);
    }
}
