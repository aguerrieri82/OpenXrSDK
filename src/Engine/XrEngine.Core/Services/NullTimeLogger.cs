using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine
{
    public class NullTimeLogger : ITimeLogger
    {
        public void Checkpoint(string name, Color color)
        {

        }

        public void Clear()
        {

        }

        public void LogValue<T>(string name, T value)
        {

        }
    }
}
