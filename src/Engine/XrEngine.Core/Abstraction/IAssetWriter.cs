namespace XrEngine
{
    public interface IAssetWriter
    {
        bool CanHandle(EngineObject obj);

        void SaveAsset(EngineObject obj, Stream stream);
    }
}
