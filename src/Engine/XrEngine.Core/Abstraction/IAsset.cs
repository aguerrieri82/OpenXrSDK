namespace XrEngine
{

    public interface IAsset
    {
        public EngineObject Load();

        public void Update(EngineObject destObj);

        public void Delete();

        public void Rename(string newName);

        public Type Type { get; }

        public string Name { get; }

        public Uri Source { get; }

        public IAssetLoaderOptions? Options { get; }
    }
}
