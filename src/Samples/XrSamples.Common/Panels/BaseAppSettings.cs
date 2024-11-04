using System.Text.Json;
using XrEngine;

namespace XrSamples
{
    public abstract class BaseAppSettings
    {
        protected string? _filePath;

        public abstract void Apply(Scene3D scene);

        public void Save()
        {
            if (_filePath == null)
                throw new InvalidOperationException();

            Save(_filePath);
        }

        public void Save(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(this, GetType());
            File.WriteAllText(filePath, json);
        }


        public void Load(string filePath)
        {
            _filePath = filePath;
            if (File.Exists(filePath))
            {
                var obj = JsonSerializer.Deserialize(File.ReadAllText(filePath), GetType());
                foreach (var prop in GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                    prop.SetValue(this, prop.GetValue(obj));
            }
        }
    }
}
