using System.Text.Json;
using System.Text.Json.Serialization;

namespace XrEngine
{
    public class EngineAssetTranscoder : BaseAssetLoader, IAssetWriter
    {
        class EngineObjectHeader
        {
            [JsonPropertyName("$type")]
            public string? TypeName { get; set; }

            public string? Name { get; set; }
        }

        public void SaveAsset(EngineObject obj, Stream stream)
        {
            JsonStateContainer state = new JsonStateContainer();
            obj.GetState(state);
            using StreamWriter writer = new StreamWriter(stream);
            writer.Write(state.AsJson());
        }

        public override EngineObject LoadAsset(Uri uri, Type resType, EngineObject? destObj, IAssetLoaderOptions? options = null)
        {
            string path = GetFilePath(uri);
            string json = File.ReadAllText(path);
            JsonStateContainer state = new JsonStateContainer(json);
            EngineObject obj = (EngineObject)ObjectManager.Instance.CreateObject(state.ReadTypeName()!);
            obj.SetState(state);
            return obj;
        }

        public override bool CanHandle(Uri uri, out Type resType)
        {
            resType = typeof(void);
            string path = GetFilePath(uri);
            if (Path.GetExtension(path) == ".eobj")
            {
                using FileStream stream = File.OpenRead(path);
                EngineObjectHeader? header = JsonSerializer.Deserialize<EngineObjectHeader>(stream);
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
