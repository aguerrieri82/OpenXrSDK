using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using XrEngine;

namespace XrSamples
{
    public abstract class BaseAppSettings
    {
        string? _filePath;

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
            File.WriteAllText(filePath, JsonSerializer.Serialize(this));
        }


        public void Load(string filePath)
        {
            _filePath = filePath;
            if (File.Exists(filePath))
            {

            }
        }

    }
}
