using System.Text.Json;
using XrEngine;

namespace XrSamples
{
    public abstract class BaseAppSettings<T> where T : EngineObject
    {
        static JsonSerializerOptions JSON_OPTIONS = new JsonSerializerOptions()
        {
            IncludeFields = true
        };

        protected string? _filePath;

        public abstract void Apply(T scene);

        public virtual void Save()
        {
            if (_filePath == null)
                return;

            Save(_filePath);
        }

        public void Save(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(this, GetType(), JSON_OPTIONS);
            File.WriteAllText(filePath, json);
        }

        public void Load(string filePath)
        {
            _filePath = filePath;
            if (File.Exists(filePath))
            {
                var obj = JsonSerializer.Deserialize(File.ReadAllText(filePath), GetType(), JSON_OPTIONS);
                foreach (var prop in GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                    prop.SetValue(this, prop.GetValue(obj));
            }
        }
    }
}
