namespace XrEngine
{
    public interface IMorphedMesh
    {
        float[] Weights { get; set; }

        long MorphVersion { get; }
    }
}
