using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace XrEngine.Lighting
{
    public class VoxelMeshView : TriangleMesh
    {
        private MeshVoxelMaterial _mat;

        public VoxelMeshView()
        {
            Geometry = Quad3D.Default;

            _mat = new MeshVoxelMaterial()
            {
                IsRemapMode = true
            };

            Materials.Add(_mat);
        }

        public void SetTarget(TriangleMesh mesh, MeshVoxelGrid grid, VoxelGridDesc desc)
        {
            _mat.Target = mesh;
            _mat.GridDesc = desc;
            _mat.FaceInstances = grid.ExtractFrontFaces();
            InstanceCount = _mat.FaceInstances.Length;

            _mat.NotifyChanged(ChangeType.Render);
            NotifyChanged(ChangeType.Render);
        }


        public VoxelFaceInstance[]? FaceInstances => _mat.FaceInstances;

        public VoxelResolvedFace[]? ResolvedFaces()
        {
            if (_mat.ResolvedFace == null || _mat.ResolvedFace.SizeBytes == 0)
                return null;
            
            var size = new VoxelResolvedFace[_mat.ResolvedFace.SizeBytes / Marshal.SizeOf<VoxelResolvedFace>()];

            var result = new VoxelResolvedFace[0];
            _mat.ResolvedFace.ReadArray(ref result);
            return result;
        }
    }
}
