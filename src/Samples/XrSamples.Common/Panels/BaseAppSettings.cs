using System.Reflection;
using System.Text.Json;
using XrEngine;

namespace XrSamples
{
    public abstract class BaseAppSettings
    {
        protected string? _filePath;

        public abstract void Apply(Scene3D scene);

        public virtual void Save()
        {
            if (_filePath == null)
                return;

            Save(_filePath);
        }

        public void Save(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath)!;
            Directory.CreateDirectory(dir);
            string json = JsonSerializer.Serialize(this, GetType());
            File.WriteAllText(filePath, json);
        }


        public void Load(string filePath)
        {
            _filePath = filePath;
            if (File.Exists(filePath))
            {
                object? obj = JsonSerializer.Deserialize(File.ReadAllText(filePath), GetType());
                foreach (PropertyInfo prop in GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                    prop.SetValue(this, prop.GetValue(obj));
            }
        }
    }
}
