using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RangeAttribute : Attribute
    {
        public RangeAttribute(float min, float max, float step)
        {
            Min = min;
            Max = max;
            Step = step;    
        }

        public float Min { get; set; }

        public float Max { get; set; }

        public float Step { get; set; }
    }
}
