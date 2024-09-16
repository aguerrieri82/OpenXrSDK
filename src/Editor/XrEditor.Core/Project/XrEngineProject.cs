using XrEngine;

namespace XrEditor
{
    public class XrEngineProject
    {
        string? _basePath;

        public XrEngineProject()
        {
            Current = this;
        }

        public void Create(string basePath)
        {
            _basePath = basePath;
        }


        public void Load(string basePath)
        {
            _basePath = basePath;
        }

        public void Import(IAsset asset)
        {
        }


        public string? BasePath => _basePath;


        public static XrEngineProject? Current { get; private set; }
    }
}
