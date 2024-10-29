using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public interface ITimeLogger
    {
        void LogValue<T>(string name, T value);

        void Checkpoint(string name);
    }
}
