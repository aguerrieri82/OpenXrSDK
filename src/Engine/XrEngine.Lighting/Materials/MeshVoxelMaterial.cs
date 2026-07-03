using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using XrMath;

namespace XrEngine.Lighting
{


    [StructLayout(LayoutKind.Explicit, Size = 48)]
    public struct GpuVoxelResolvedFace
    {
        [FieldOffset(0)]
        public Vector4 BaseColor;

        [FieldOffset(16)]
        public Vector3 Normal;

        [FieldOffset(28)]
        public float Roughness;

        [FieldOffset(32)]
        public float Metallic;
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct GpuVoxelFaceInstance
    {
        [FieldOffset(0)]
        public Vector3I Pos;

        [FieldOffset(12)]
        public int Face;

        [FieldOffset(16)]
        public Vector2 UV;

        [FieldOffset(24)]
        public int TriangleId;
    }


    public class MeshVoxelMaterial : DynamicMaterial
    {
        private IBuffer<GpuVoxelResolvedFace>? _resolveBuffer;

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

                    _resolveBuffer ??= ctx.BufferProvider!.GetBuffer<GpuVoxelResolvedFace>(10, BufferStore.Material, BufferUsage.SSbo);

                    var size = (uint)(Marshal.SizeOf<GpuVoxelResolvedFace>() * FaceInstances!.Length);

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


        public GpuVoxelResolvedFace[]? ReadResolvedFaces()
        {
            if (ResolvedFace == null || ResolvedFace.SizeBytes == 0)
                return null;

            var size = new GpuVoxelResolvedFace[ResolvedFace.SizeBytes / Marshal.SizeOf<GpuVoxelResolvedFace>()];

            var result = Array.Empty<GpuVoxelResolvedFace>();
            
            ResolvedFace.ReadArray(ref result);

            return result;
        }

        public IBuffer<VertexData>? TargetVBuf { get; set; }

        public IBuffer<uint>? TargetIBuf { get; set; }

        public IBuffer<GpuVoxelResolvedFace>? ResolvedFace => _resolveBuffer;

        public GpuVoxelFaceInstance[]? FaceInstances { get; set; }

        public bool IsRemapMode { get; set; }

        public bool IsVoxelPreview { get; set; }

        public bool IsPreviewMaterial { get; set; }

        public bool UseGeoTriNormal { get; set; }

        public VoxelGridDesc GridDesc { get; set; }

        public TriangleMesh? Target { get; set; }


    }
}
