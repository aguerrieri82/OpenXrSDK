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

            if (IsVoxelPreview)
                bld.AddFeature("VOXEL_PREVIEW");

            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uViewProj", ctx.PassCamera!.ViewProjection);
                up.SetUniform("uGridOrigin", GridDesc.Origin);
                up.SetUniform("uVoxelSize", GridDesc.VoxelSize);
                up.SetUniform("uCameraPosition", ctx.PassCamera!.WorldPosition);

            });

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

        public IBuffer<GpuVoxelResolvedFace>? ResolvedFace => _resolveBuffer;

        public GpuVoxelFaceInstance[]? FaceInstances { get; set; }

        public bool IsVoxelPreview { get; set; }

        public bool UseGeoTriNormal { get; set; }

        public VoxelGridDesc GridDesc { get; set; }
    }
}
