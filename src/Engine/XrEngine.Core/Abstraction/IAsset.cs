using SkiaSharp;

namespace XrEngine
{

    public interface IAsset
    {
        public void Update(EngineObject destObj);

        public void Delete();

        public void Rename(string rename);

        public Type Type { get; }

        public string Name { get; }

        public Uri Source { get; }

        public object Options { get; }  
    }
}
