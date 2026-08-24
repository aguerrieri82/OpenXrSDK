using System.Numerics;
using System.Text;
using XrEngine.Components;

namespace XrEngine
{
    public static class MorphVertexShader
    {
        public static void UpdateShader(ShaderUpdateBuilder bld)
        {
            if (bld.Context.Model is not TriangleMesh triMesh)
                throw new NotSupportedException();

            var mesh = bld.Context.Model.Feature<IMorphedMesh>()
                     ?? throw new NotSupportedException();

            var geo = triMesh.Geometry?.Component<MorphedGeometry>()
                   ?? throw new NotSupportedException();

            Generate(bld, mesh, geo);
        }

        public static bool NeedUpdateShader(UpdateShaderContext ctx)
        {
            long InnerWeightMask()
            {
                var morph = ctx.Model?.ComponentsDeep<MeshMorph>().FirstOrDefault();

                if (morph == null)
                    return 0;

                return WeightMask(morph.Weights);
            }

            var material = ctx.Material!;

            return material._tracker.IsChanged(() => material.UseMorph) ||
                   material._tracker.IsChanged(() => material.Morph) ||
                   (material.UseMorph &&
                   material.Morph == MorphMode.NotEmptyTargets &&
                   material._tracker.IsChanged(() => InnerWeightMask()));
        }

        static long WeightMask(float[] weights)
        {
            long mask = 0;

            for (var i = 0; i < weights.Length; i++)
            {
                if (weights[i] != 0f)
                    mask |= 1L << i;
            }

            return mask;
        }

        static void Generate(ShaderUpdateBuilder bld, IMorphedMesh morphMesh, MorphedGeometry morphGeo)
        {
            if (morphGeo.Targets == null || morphMesh.Weights.Length == 0)
                return;

            var targetCount = Math.Min(morphMesh.Weights.Length, morphGeo.Targets.Length);

            if (targetCount > MorphUniforms.MaxTargets)
                throw new InvalidOperationException();

            var mode = bld.Context.Material!.Morph;

            var storage = morphGeo.StorageType == MorphStorageType.Auto
                ? MorphStorageType.Texture
                : morphGeo.StorageType;

            var weightMask = mode == MorphMode.NotEmptyTargets
                ? WeightMask(morphMesh.Weights)
                : 0L;

            bld.AddFeature("USE_MORPH");
            bld.AddFeature($"MAX_MORPH_TARGETS {MorphUniforms.MaxTargets}");

            if (mode == MorphMode.DynamicTargets)
                bld.AddFeature("MORPH_DYNAMIC_TARGETS");

            uint textureWidth = 0;

            if (storage == MorphStorageType.Ssbo)
            {
                bld.AddFeature("USE_MORPH_SSBO");

                bld.LoadBuffer<Vector3>(ctx =>
                {
                    var buffer = (IBuffer<Vector3>)ctx.CurrentBuffer!;

                    if (buffer.Version != morphGeo.Host!.Version)
                    {
                        morphGeo.UpdateBuffer(buffer);
                        buffer.Version = morphGeo.Host.Version;
                    }

                    return null;

                }, BufferSlots.Morph, BufferStore.Model, BufferUsage.SSbo);
            }
            else if (storage == MorphStorageType.Texture)
            {
                bld.AddFeature("USE_MORPH_TEXTURE");

                var texture = morphGeo.UpdateTexture();
                textureWidth = texture.Width;

                bld.LoadTexture(ctx => texture, TextureSlots.Morph);
            }
            else
            {
                throw new NotSupportedException();
            }

            for (var targetIndex = 0; targetIndex < targetCount; targetIndex++)
            {
                if (mode == MorphMode.NotEmptyTargets &&
                    (weightMask & (1L << targetIndex)) == 0)
                    continue;

                foreach (var component in morphGeo.Targets[targetIndex].Components)
                    bld.AddFeature($"MORPH_{targetIndex}_{component.Component}");
            }

            bld.SetSlot(ShaderSlots.ApplyMorph, () => GenerateSource(morphGeo, targetCount, mode, weightMask));

            var unif = new MorphUniforms();
            var offsetsInitialized = false;

            void SetOffset(ref MorphTargetUniform target, VertexComponent component, uint offset)
            {
                switch (component)
                {
                    case VertexComponent.MorphPosition:
                        target.PositionOfs = offset;
                        break;

                    case VertexComponent.MorphNormal:
                        target.NormalOfs = offset;
                        break;

                    case VertexComponent.MorphTangent:
                        target.TangentOfs = offset;
                        break;

                    default:
                        throw new NotSupportedException();
                }
            }

            void InitOffsets()
            {
                if (storage == MorphStorageType.Ssbo)
                {
                    uint ofs = 0;

                    for (var targetIndex = 0; targetIndex < morphGeo.Targets.Length; targetIndex++)
                    {
                        var target = morphGeo.Targets[targetIndex];

                        foreach (var component in target.Components)
                        {
                            if (targetIndex < targetCount)
                                SetOffset(ref unif.Targets[targetIndex], component.Component, ofs);

                            ofs += (uint)component.Values.Length;
                        }
                    }
                }
                else
                {
                    uint row = 0;

                    for (var targetIndex = 0; targetIndex < morphGeo.Targets.Length; targetIndex++)
                    {
                        var target = morphGeo.Targets[targetIndex];

                        foreach (var component in target.Components)
                        {
                            if (targetIndex < targetCount)
                                SetOffset(ref unif.Targets[targetIndex], component.Component, row);

                            row += ((uint)component.Values.Length + textureWidth - 1) / textureWidth;
                        }
                    }
                }

                offsetsInitialized = true;
            }

            bld.LoadBuffer<MorphUniforms>(ctx =>
            {
                var curVer = morphGeo.Host!.Version + morphMesh.MorphVersion;

                if (ctx.CurrentBuffer!.Version == curVer)
                    return null;

                if (!offsetsInitialized)
                    InitOffsets();

                for (var i = 0; i < targetCount; i++)
                    unif.Targets[i].Weight = morphMesh.Weights[i];

                ctx.CurrentBuffer.Version = curVer;

                return unif;

            }, UniformsSlots.Morph, BufferStore.Model, BufferUsage.Uniforms);
        }

        static string GenerateSource(
            MorphedGeometry morphGeo,
            int targetCount,
            MorphMode mode,
            long weightMask)
        {
            var builder = new StringBuilder();

            builder.AppendLine("void applyMorph(inout vec3 position, inout vec3 normal");
            builder.AppendLine("#ifdef HAS_TANGENTS");
            builder.AppendLine("    , inout vec3 tangent");
            builder.AppendLine("#endif");
            builder.AppendLine(")");
            builder.AppendLine("{");
            builder.AppendLine("    morphInit();");

            for (var targetIndex = 0; targetIndex < targetCount; targetIndex++)
            {
                if (mode == MorphMode.NotEmptyTargets &&
                    (weightMask & (1L << targetIndex)) == 0)
                    continue;

                var target = morphGeo.Targets![targetIndex];

                if (mode == MorphMode.DynamicTargets)
                {
                    builder.AppendLine(
                        $"    if (uMorphTargets[{targetIndex}].weight != 0.0)");
                    builder.AppendLine("    {");
                }

                var indent = mode == MorphMode.DynamicTargets
                    ? "        "
                    : "    ";

                foreach (var component in target.Components)
                {
                    switch (component.Component)
                    {
                        case VertexComponent.MorphPosition:
                            builder.AppendLine(
                                $"{indent}position += morphFetch(uMorphTargets[{targetIndex}].positionOfs) * uMorphTargets[{targetIndex}].weight;");
                            break;

                        case VertexComponent.MorphNormal:
                            builder.AppendLine(
                                $"{indent}normal += morphFetch(uMorphTargets[{targetIndex}].normalOfs) * uMorphTargets[{targetIndex}].weight;");
                            break;

                        case VertexComponent.MorphTangent:
                            builder.AppendLine("#ifdef HAS_TANGENTS");
                            builder.AppendLine(
                                $"{indent}tangent += morphFetch(uMorphTargets[{targetIndex}].tangentOfs) * uMorphTargets[{targetIndex}].weight;");
                            builder.AppendLine("#endif");
                            break;

                        default:
                            throw new NotSupportedException();
                    }
                }

                if (mode == MorphMode.DynamicTargets)
                    builder.AppendLine("    }");
            }

            builder.AppendLine("}");

            return builder.ToString();
        }
    }
}