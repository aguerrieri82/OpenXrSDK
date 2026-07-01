using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace XrEngine.Lighting
{



    public class MeshVoxelMaterial : DynamicMaterial
    {
        private IBuffer<VoxelResolvedFace>? _resolveBuffer;

        public MeshVoxelMaterial():
            base("[XrEngine.Lighting]mesh_voxel.vert", "[XrEngine.Lighting]mesh_voxel.frag")
        {
            IsVoxelPreview = true;
            DoubleSided = true;
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            if (UseGeoTriNormal)
                bld.AddFeature("USE_GEOMETRIC_TRI_NORMAL");

            if (IsPreviewMaterial)
                bld.AddFeature("PREVIEW_MATERIAL");
            
            if (IsVoxelPreview)
                bld.AddFeature("VOXEL_PREVIEW");

            if (IsRemapMode)
                bld.AddFeature("VOXEL_REMAP");

            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uViewProj", ctx.PassCamera!.ViewProjection);
                up.SetUniform("uGridOrigin", GridDesc.Origin);
                up.SetUniform("uVoxelSize", GridDesc.VoxelSize);
                up.SetUniform("uCameraPosition", ctx.PassCamera!.WorldPosition);

            });

            var mat = Target!.Materials.OfType<PbrV2Material>().FirstOrDefault();


            if ((IsRemapMode | IsPreviewMaterial) && mat != null)
            {
                if (mat.ColorMap != null)
                {
                    bld.AddFeature("HAS_BASE_COLOR_MAP");
                    bld.LoadTexture(ctx => mat.ColorMap, TextureSlots.Albedo);
                }

                if (mat.NormalMap != null)
                {
                    bld.AddFeature("HAS_NORMAL_MAP");
                    bld.LoadTexture(ctx => mat.NormalMap, TextureSlots.Normal);
                }

                if (mat.MetallicRoughnessMap != null)
                {
                    bld.AddFeature("HAS_METALLIC_ROUGHNESS_MAP");
                    bld.LoadTexture(ctx => mat.MetallicRoughnessMap, TextureSlots.MetallicRoughness);
                }

                bld.ExecuteAction((ctx, up) =>
                {
                    up.SetUniform("uBaseColorFactor", mat.Color);
                    up.SetUniform("uMetallicFactor", mat.Metalness);
                    up.SetUniform("uRoughnessFactor", mat.Roughness);
                    up.SetUniform("uCameraPosition", ctx.PassCamera!.WorldPosition);

                    _resolveBuffer ??= ctx.BufferProvider!.GetBuffer<VoxelResolvedFace>(10, BufferStore.Material, BufferUsage.SSbo);

                    var size = (uint)(Marshal.SizeOf<VoxelResolvedFace>() * FaceInstances!.Length);

                    if (_resolveBuffer.SizeBytes != size)
                        _resolveBuffer.Allocate(size);

                    up.LoadBuffer(_resolveBuffer, 10);

                    if (TargetVBuf != null)
                        up.LoadBuffer(TargetVBuf, 12, BufferUsage.SSbo);

                    if (TargetIBuf != null)
                        up.LoadBuffer(TargetIBuf, 13, BufferUsage.SSbo);


                });
            }

            bld.LoadBufferArray(ctx =>
            {
                var verision = ContentVersion + Version;

                if (ctx.CurrentBuffer!.Version == verision)
                    return null;
                
                ctx.CurrentBuffer.Version = verision;
                
                return FaceInstances;

            },11, BufferStore.Material, BufferUsage.SSbo);

            base.UpdateShaderMaterial(bld);
        }


        public VoxelResolvedFace[]? ReadResolvedFaces()
        {
            if (ResolvedFace == null || ResolvedFace.SizeBytes == 0)
                return null;

            var size = new VoxelResolvedFace[ResolvedFace.SizeBytes / Marshal.SizeOf<VoxelResolvedFace>()];

            var result = Array.Empty<VoxelResolvedFace>();
            
            ResolvedFace.ReadArray(ref result);

            return result;
        }

        public IBuffer<VertexData>? TargetVBuf { get; set; }

        public IBuffer<uint>? TargetIBuf { get; set; }

        public IBuffer<VoxelResolvedFace>? ResolvedFace => _resolveBuffer;

        public VoxelFaceInstance[]? FaceInstances { get; set; }

        public bool IsRemapMode { get; set; }

        public bool IsVoxelPreview { get; set; }

        public bool IsPreviewMaterial { get; set; }

        public bool UseGeoTriNormal { get; set; }

        public VoxelGridDesc GridDesc { get; set; }

        public TriangleMesh? Target { get; set; }


    }
}
