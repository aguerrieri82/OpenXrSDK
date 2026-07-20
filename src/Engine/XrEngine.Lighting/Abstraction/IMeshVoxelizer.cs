namespace XrEngine.Lighting
{
    public interface IMeshVoxelizer
    {
        IList<GpuVoxelFaceData> Voxelize(IReadOnlyList<TriangleMesh> meshes);
    }
}
