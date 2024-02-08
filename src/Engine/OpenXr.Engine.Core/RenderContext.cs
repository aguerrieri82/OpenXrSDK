using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class RenderContext
    {
        public TimeSpan StartTime { get; internal set; }

        public long Frame { get; internal set; }

        public double Time { get; internal set; }
    }
}
