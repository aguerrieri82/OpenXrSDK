namespace XrEngine
{
    public class MergedAssetStore : IAssetStore
    {
        readonly List<IAssetStore> _stores;

        public MergedAssetStore()
        {
            _stores = [];
        }

        public bool Contains(string name)
        {
            return _stores.Any(a => a.Contains(name));
        }

        IAssetStore FindStore(string name)
        {
            return _stores.First(a => a.Contains(name));
        }

        public string GetPath(string name)
        {
            var store = FindStore(name);
            return store.GetPath(name);
        }

        public IEnumerable<string> List(string storePath)
        {
            return _stores.SelectMany(a => a.List(storePath));
        }

        public IEnumerable<string> ListDirectories(string storePath)
        {
            return _stores.SelectMany(a => a.ListDirectories(storePath));
        }

        public Stream Open(string name)
        {
            var store = FindStore(name);
            return store.Open(name);
        }

        public static MergedAssetStore FromLocalPaths(params string[] paths)
        {
            var store = new MergedAssetStore();
            store._stores.AddRange(paths.Select(a => new LocalAssetStore(a)));
            return store;
        }
    }
}
