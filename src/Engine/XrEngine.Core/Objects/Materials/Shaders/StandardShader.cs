using System.Diagnostics;
using System.Numerics;
using XrMath;

namespace XrEngine
{

    public class StandardShader : Shader, IShaderHandler, IInstanceShader
    {
        protected readonly ChangeTracker _tracker = new();

        public StandardShader()
        {
            VertexSourceName = "standard.vert";
            Resolver = str => Embedded.GetString(str);
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
            bld.LoadBuffer<ModelUniforms>((ctx, ref update) =>
            {
                Debug.Assert(ctx.Model != null);

                var curVersion = ctx.Model.Transform.Version;

                var motVectActive = ctx.UseMotionVectors && ctx.MotionVectorProvider?.IsActive == true;

                if (curVersion == ctx.CurrentBuffer!.Version && !motVectActive)
                    return false;

                ctx.CurrentBuffer!.Version = curVersion;

                var worldMatrix = ctx.Model.WorldMatrix;
                var prevWorldMatrix = ctx.MotionVectorProvider?.GetPrevMatrix(ctx.Model) ?? worldMatrix;

                if (ctx.Model is ICompressedVertexSource cmp)
                {
                    worldMatrix = cmp.VerticesRemap * worldMatrix;
                    prevWorldMatrix = cmp.VerticesRemap * prevWorldMatrix;
                }

                update.Value = new ModelUniforms
                {
                    NormalMatrix = ctx.Model.NormalMatrix,
                    PrevWorldMatrix = prevWorldMatrix,
                    WorldMatrix = worldMatrix,
                };

                return true;    

            }, UniformsSlots.Model, BufferStore.Model,
               bld.Context.UseSharedSsbo ? BufferUsage.SharedSsbo : BufferUsage.Uniforms, "uModelIndex");
        }

        bool IInstanceShader.NeedUpdate(Object3D model, long curVersion)
        {
            model.EnsureTransformUpdate();
            return model.Transform.Version != curVersion;
        }

        unsafe long IInstanceShader.Update(UpdateShaderContext ctx, byte* destData, Object3D model, int drawId)
        {
            var worldMatrix = model.WorldMatrix;
            var prevWorldMatrix = ctx.MotionVectorProvider?.GetPrevMatrix(model) ?? worldMatrix;

            if (model is ICompressedVertexSource cmp)
            {
                worldMatrix = cmp.VerticesRemap * worldMatrix;
                prevWorldMatrix = cmp.VerticesRemap * prevWorldMatrix;
            }

            *(ModelUniforms*)destData = new ModelUniforms
            {
                NormalMatrix = model.NormalMatrix,
                WorldMatrix = worldMatrix,
                PrevWorldMatrix = prevWorldMatrix,
                DrawId = drawId
            };

            return model.Transform.Version;
        }

        protected virtual void UpdateShaderGlobal(ShaderUpdateBuilder bld)
        {
            var shadowOpt = bld.Context.ShadowMapProvider?.Options;

            var shadowMode = shadowOpt?.Mode ?? ShadowMapMode.None;

            if (bld.Context.ClipRegions != null && bld.Context.ClipMode == ShaderClipMode.VertexClipCull )
            {
                bld.AddExtension("GL_EXT_clip_cull_distance");

                bld.AddFeature("USE_VIEW_CLIP");

                bld.ExecuteAction((ctx, up) =>
                {
                    var clips = ctx.ClipRegions;

                    if (clips == null)
                        return;

                    var size = ctx.PassCamera!.ViewSize;

                    for (var j = 0; j < clips.Length; j++)
                    {
                        var clip = clips[j];

                        var minX = 2f * clip.X / size.Width - 1f;
                        var minY = 2f * clip.Y / size.Height - 1f;
                        var maxX = 2f * (clip.X + clip.Width) / size.Width - 1f;
                        var maxY = 2f * (clip.Y + clip.Height) / size.Height - 1f;

                        up.SetUniform($"uViewClip[{j}]", new Vector4(minX, minY, maxX, maxY));
                    }
                });
            }

            if (bld.Context.UseSharedSsbo)
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

            bld.LoadBuffer<CameraUniforms>((ctx, ref update) =>
            {
                Debug.Assert(ctx.PassCamera != null);

                update.Value = new CameraUniforms
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
                    update.Value.LightSpaceMatrix = light.Value;

                return true;

            }, UniformsSlots.Camera, BufferStore.Shader);

            if (bld.Context.UseMotionVectors &&
                UseMotionVectors &&
                bld.Context.MotionVectorProvider?.IsActive == true)
            {
                bld.AddFeature("MOTION_VECTORS");

                if (bld.Context.CopyDepthImage?.Tag is IMotionVectorProvider)
                    bld.AddFeature("MOTION_VECTORS_DEPTH");

                bld.ExecuteAction((ctx, up) =>
                {
                    var texture = ctx.MotionVectorProvider?.Texture;

                    if (texture != null)
                    {
                        var size = new Vector2(texture.Width, texture.Height);
                        var scale = size / ctx.PassCamera!.ViewSize.ToVector2();

                        up.SetUniform("uMotionImageScale", scale);

                        up.LoadImage(texture, ImagesSlots.MotionVectors, ctx.UseManualDepthTest ?
                            BufferAccessMode.Read : BufferAccessMode.Write);
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

            if (bld.Context.UseCopyDepth)
                bld.AddFeature("COPY_DEPTH");

            if (bld.Context.UseManualDepthTest)
                bld.AddFeature("MANUAL_DEPTH_TEST");

            if (bld.Context.UsePrimitiveBoundingBox)
                bld.AddExtension("GL_EXT_primitive_bounding_box");

            if (bld.Context.CopyDepthImage != null && bld.Context.CopyDepthImage.Tag == null)
            {
                bld.AddFeature("COPY_DEPTH_IMG");

                bld.ExecuteAction((ctx, up) =>
                {
                    Debug.Assert(ctx.CopyDepthImage?.Tag == null);

                    if (ctx.CopyDepthImage == null)
                        return;

                    up.LoadImage(ctx.CopyDepthImage, ImagesSlots.Depth, ctx.UseManualDepthTest ?
                        BufferAccessMode.Read : BufferAccessMode.Write);

                    var size = new Vector2(ctx.CopyDepthImage.Width, ctx.CopyDepthImage.Height);
                    var scale = size / ctx.PassCamera!.ViewSize.ToVector2();

                    up.SetUniform("uDepthImageScale", scale);
                });
            }
        }

        public virtual bool NeedUpdateShader(UpdateShaderContext ctx)
        {
            return _tracker.IsChanged(() => ctx.UseMotionVectors) ||
                   _tracker.IsChanged(() => ctx.UseManualDepthTest) ||
                   _tracker.IsChanged(() => ctx.UsePrimitiveBoundingBox) ||
                   _tracker.IsChanged(() => ctx.CopyDepthImage?.Tag) ||
                   _tracker.IsChanged(() => ctx.ClipRegions != null && ctx.ClipRegions.Length > 0) ||
                   _tracker.IsChanged(() => ctx.MotionVectorProvider?.IsActive ?? false);
        }

        public static readonly StandardShader Instance = new();

        public bool UseMotionVectors { get; set; }

        public Type InstanceBufferType => typeof(ModelUniforms);
    }
}
