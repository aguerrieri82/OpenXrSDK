using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Lighting
{
    public interface IMeshVoxelizer
    {
        IList<GpuVoxelFaceData> Voxelize(IReadOnlyList<TriangleMesh> meshes);
    }
}
