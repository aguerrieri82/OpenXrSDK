using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine
{
    public interface ITimeLogger
    {
        void LogValue<T>(string name, T value);

        void Checkpoint(string name, Color color);

        void Clear();
    }
}
