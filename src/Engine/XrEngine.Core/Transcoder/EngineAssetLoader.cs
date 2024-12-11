using System.Text.Json;
using System.Text.Json.Serialization;
using XrEngine.Services;
using XrEngine.Transcoder;

namespace XrEngine
{
    public class EngineAssetLoader : BaseAssetLoader, IAssetWriter
    {
        class EngineObjectHeader
        {
            [JsonPropertyName("$type")]
            public string? TypeName { get; set; }

            public string? Name { get; set; }
        }

        public void SaveAsset(EngineObject obj, Stream stream)
        {
            var state = new JsonStateContainer();
            obj.GetState(state);
            using var writer = new StreamWriter(stream);
            writer.Write(state.AsJson());
        }

        public override EngineObject LoadAsset(Uri uri, Type resType, EngineObject? destObj, IAssetLoaderOptions? options = null)
        {
            var path = GetFilePath(uri);
            var json = File.ReadAllText(path);
            var state = new JsonStateContainer(json);
            var obj = (EngineObject)ObjectManager.Instance.CreateObject(state.ReadTypeName()!);
            obj.SetState(state);
            return obj;
        }

        public override bool CanHandle(Uri uri, out Type resType)
        {
            resType = typeof(void);
            var path = GetFilePath(uri);
            if (Path.GetExtension(path) == ".eobj")
            {
                using var stream = File.OpenRead(path);
                var header = JsonSerializer.Deserialize<EngineObjectHeader>(stream);
                if (header?.TypeName == null)
                    return false;
                resType = ObjectManager.Instance.FindType(header.TypeName)!;
                return true;
            }
            return false;
        }

        public bool CanHandle(EngineObject obj)
        {
            return true;
        }

    }
}
