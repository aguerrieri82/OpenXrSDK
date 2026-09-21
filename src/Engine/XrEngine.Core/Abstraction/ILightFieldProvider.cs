namespace XrEngine
{
    [Flags]
    public enum LightFieldVersion
    {
        None = 0,
        V1 = 1,
        V2 = 2
    }

    public interface ILightFieldProvider
    {
        LightFieldVersion Versions { get; }

        LightFieldData GetLightField();

        LightFieldDataV2 GetLightFieldV2();
    }
}   
