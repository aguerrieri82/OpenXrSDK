using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace XrEngine.Objects.Materials.Shaders
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MorphTarget
    {
        public float Weight;
        public uint PositionOfs;
        public uint NormalOfs;
        public uint TangentOfs;
    }

    [InlineArray(MorphUniforms.MaxTargets)]
    public struct MorphTargetArray
    {
        private MorphTarget _element0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MorphUniforms
    {
        public const int MaxTargets = 10;

        public MorphTargetArray Targets;
    }

    public static class MorphVertexShader
    {
        public static void UpdateShader(ShaderUpdateBuilder bld)
        {
            if (bld.Context.Material == null || !bld.Context.Material.HasMorph)
                return;

            if (bld.Context.Model is not TriangleMesh triMesh)
                throw new NotSupportedException();

            var mesh = bld.Context.Model.Feature<IMorphedMesh>()
                     ?? throw new NotSupportedException();

            var geo = triMesh.Geometry?.Component<MorphedGeometry>()
                   ?? throw new NotSupportedException();

            Generate(bld, mesh, geo);
        }

        static void Generate(ShaderUpdateBuilder bld, IMorphedMesh morphMesh, MorphedGeometry morphGeo)
        {
            if (morphGeo.Targets == null || morphMesh.Weights.Length == 0)
                return;

            var targetCount = Math.Min(morphMesh.Weights.Length, morphGeo.Targets.Length);

            if (targetCount > MorphUniforms.MaxTargets)
                throw new InvalidOperationException();

            var unif = new MorphUniforms();
            var builder = new StringBuilder();

            bld.AddFeature("USE_MORPH");
            bld.AddFeature($"MAX_MORPH_TARGETS {MorphUniforms.MaxTargets}");

            var storage = morphGeo.StorageType == MorphStorageType.Auto
                ? MorphStorageType.Texture
                : morphGeo.StorageType;

            if (storage == MorphStorageType.Ssbo)
            {
                bld.AddFeature("USE_MORPH_SSBO");

                uint ofs = 0;

                for (var targetIndex = 0; targetIndex < morphGeo.Targets.Length; targetIndex++)
                {
                    var target = morphGeo.Targets[targetIndex];

                    foreach (var component in target.Components)
                    {
                        if (targetIndex < targetCount)
                        {
                            ref var dst = ref unif.Targets[targetIndex];

                            switch (component.Component)
                            {
                                case VertexComponent.MorphPosition:
                                    dst.PositionOfs = ofs;
                                    break;

                                case VertexComponent.MorphNormal:
                                    dst.NormalOfs = ofs;
                                    break;

                                case VertexComponent.MorphTangent:
                                    dst.TangentOfs = ofs;
                                    break;
                                default:
                                    throw new NotSupportedException();
                            }
                        }

                        ofs += (uint)component.Values.Length;
                    }
                }

                bld.LoadBuffer<Vector3>(ctx =>
                {
                    var buffer = (IBuffer<Vector3>)ctx.CurrentBuffer!;

                    if (buffer.Version != morphGeo.Host!.Version)
                    {
                        morphGeo.UpdateBuffer(buffer);
                        buffer.Version = morphGeo.Host.Version;
                    }

                    return null;

                }, BufferSlots.MorphSSbo, BufferStore.Model, BufferUsage.SSbo);
            }
            else if (storage == MorphStorageType.Texture)
            {
                bld.AddFeature("USE_MORPH_TEXTURE");

                var texture = morphGeo.CreateTexture();
                var width = texture.Width;
                uint row = 0;

                for (var targetIndex = 0; targetIndex < morphGeo.Targets.Length; targetIndex++)
                {
                    var target = morphGeo.Targets[targetIndex];

                    foreach (var component in target.Components)
                    {
                        if (targetIndex < targetCount)
                        {
                            ref var dst = ref unif.Targets[targetIndex];

                            switch (component.Component)
                            {
                                case VertexComponent.MorphPosition:
                                    dst.PositionOfs = row;
                                    break;

                                case VertexComponent.MorphNormal:
                                    dst.NormalOfs = row;
                                    break;

                                case VertexComponent.MorphTangent:
                                    dst.TangentOfs = row;
                                    break;
                                default:
                                    throw new NotSupportedException();
                            }
                        }

                        var rows = ((uint)component.Values.Length + width - 1) / width;
                        row += rows;
                    }
                }

                bld.LoadTexture(ctx => texture, TextureSlots.Morph);
            }
            else
                throw new NotSupportedException();

            builder.AppendLine("void applyMorph(inout vec3 position, inout vec3 normal");
            builder.AppendLine("#ifdef HAS_TANGENTS");
            builder.AppendLine("    , inout vec3 tangent");
            builder.AppendLine("#endif");
            builder.AppendLine(")");
            builder.AppendLine("{");
            builder.AppendLine("    morphInit();");

            for (var targetIndex = 0; targetIndex < targetCount; targetIndex++)
            {
                var target = morphGeo.Targets[targetIndex];

                foreach (var component in target.Components)
                {
                    switch (component.Component)
                    {
                        case VertexComponent.MorphPosition:
                            builder.AppendLine(
                                $"    position += morphFetch(uMorphTargets[{targetIndex}].positionOfs) * uMorphTargets[{targetIndex}].weight;");
                            break;

                        case VertexComponent.MorphNormal:
                            builder.AppendLine(
                                $"    normal += morphFetch(uMorphTargets[{targetIndex}].normalOfs) * uMorphTargets[{targetIndex}].weight;");
                            break;

                        case VertexComponent.MorphTangent:
                            builder.AppendLine("#ifdef HAS_TANGENTS");
                            builder.AppendLine(
                                $"    tangent += morphFetch(uMorphTargets[{targetIndex}].tangentOfs) * uMorphTargets[{targetIndex}].weight;");
                            builder.AppendLine("#endif");
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                }
            }

            builder.AppendLine("}");

            bld.SetSlot("APPLY_MORPH", builder.ToString());

            bld.LoadBuffer<MorphUniforms>(ctx =>
            {
                var curVer = morphGeo.Host!.Version + morphMesh.MorphVersion;

                if (ctx.CurrentBuffer!.Version == curVer)
                    return null;

                for (var i = 0; i < targetCount; i++)
                    unif.Targets[i].Weight = morphMesh.Weights[i];

                ctx.CurrentBuffer.Version = curVer;

                return unif;

            }, UniformsSlots.Morph, BufferStore.Model, BufferUsage.Uniforms);
        }
    }
}