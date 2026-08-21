using System.Numerics;
using System.Runtime.InteropServices;
using XrMath;

namespace XrEngine.Lighting
{
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct GpuVoxelFaceInstance
    {
        [FieldOffset(0)]
        public Vector3I Pos;

        [FieldOffset(12)]
        public int Face;

        [FieldOffset(16)]
        public Vector4 BaseColor;

        [FieldOffset(32)]
        public Vector3 Normal;

        [FieldOffset(44)]
        public float Roughness;

        [FieldOffset(48)]
        public float Metallic;
    }

    public class MeshVoxelMaterial : DynamicMaterial
    {
        protected GpuVoxelFaceInstance[] _faces = [];

        private int _faceCount;

        public MeshVoxelMaterial()
            : base(
                "[XrEngine.Lighting]mesh_voxel.vert",
                "[XrEngine.Lighting]mesh_voxel.frag")
        {
            DoubleSided = true;
            WriteDepth = false;
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                var cameraForward = ctx.PassCamera!.Forward;

                var absX = MathF.Abs(cameraForward.X);
                var absY = MathF.Abs(cameraForward.Y);
                var absZ = MathF.Abs(cameraForward.Z);

                int axis;
                float direction;

                if (absX >= absY && absX >= absZ)
                {
                    axis = 0;
                    direction = cameraForward.X;
                }
                else if (absY >= absZ)
                {
                    axis = 1;
                    direction = cameraForward.Y;
                }
                else
                {
                    axis = 2;
                    direction = cameraForward.Z;
                }

                var axisStart = axis * _faceCount;

                int instanceStart;
                int instanceStep;

                if (direction <= 0.0f)
                {
                    instanceStart = axisStart;
                    instanceStep = 1;
                }
                else
                {
                    instanceStart = axisStart + _faceCount - 1;
                    instanceStep = -1;
                }

                up.SetUniform("uViewProjection", ctx.PassCamera.ViewProjection);
                up.SetUniform("uGridOrigin", GridDesc.Origin);
                up.SetUniform("uVoxelSize", GridDesc.VoxelSize);
                up.SetUniform("uInstanceStart", instanceStart);
                up.SetUniform("uInstanceStep", instanceStep);
            });

            bld.LoadBufferArray(
                ctx =>
                {
                    var version = ContentVersion + Version;

                    if (ctx.CurrentBuffer!.Version == version)
                        return null;

                    ctx.CurrentBuffer.Version = version;
                    return _faces;
                },
                11,
                BufferStore.Material,
                BufferUsage.SSbo);

            base.UpdateShaderMaterial(bld);
        }

        public void LoadFaces(GpuVoxelFaceInstance[] faces)
        {
            _faceCount = faces.Length;
            _faces = new GpuVoxelFaceInstance[_faceCount * 3];

            faces.CopyTo(_faces, 0);
            faces.CopyTo(_faces, _faceCount);
            faces.CopyTo(_faces, _faceCount * 2);

            Array.Sort(
                _faces,
                0,
                _faceCount,
                Comparer<GpuVoxelFaceInstance>.Create(
                    static (a, b) =>
                    {
                        var aKey = a.Pos.X * 2 +
                            (a.Face == 0 ? -1 :
                             a.Face == 1 ? 1 : 0);

                        var bKey = b.Pos.X * 2 +
                            (b.Face == 0 ? -1 :
                             b.Face == 1 ? 1 : 0);

                        var cmp = aKey.CompareTo(bKey);
                        if (cmp != 0)
                            return cmp;

                        return a.Face.CompareTo(b.Face);
                    }));

            Array.Sort(
                _faces,
                _faceCount,
                _faceCount,
                Comparer<GpuVoxelFaceInstance>.Create(
                    static (a, b) =>
                    {
                        var aKey = a.Pos.Y * 2 +
                            (a.Face == 2 ? -1 :
                             a.Face == 3 ? 1 : 0);

                        var bKey = b.Pos.Y * 2 +
                            (b.Face == 2 ? -1 :
                             b.Face == 3 ? 1 : 0);

                        var cmp = aKey.CompareTo(bKey);
                        if (cmp != 0)
                            return cmp;

                        return a.Face.CompareTo(b.Face);
                    }));

            Array.Sort(
                _faces,
                _faceCount * 2,
                _faceCount,
                Comparer<GpuVoxelFaceInstance>.Create(
                    static (a, b) =>
                    {
                        var aKey = a.Pos.Z * 2 +
                            (a.Face == 4 ? -1 :
                             a.Face == 5 ? 1 : 0);

                        var bKey = b.Pos.Z * 2 +
                            (b.Face == 4 ? -1 :
                             b.Face == 5 ? 1 : 0);

                        var cmp = aKey.CompareTo(bKey);
                        if (cmp != 0)
                            return cmp;

                        return a.Face.CompareTo(b.Face);
                    }));

            Invalidate();
        }

        public VoxelGridDesc GridDesc { get; set; }
    }
}