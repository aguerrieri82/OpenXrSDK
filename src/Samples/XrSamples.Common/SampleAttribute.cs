using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrSamples
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SampleAttribute : Attribute
    {
        public SampleAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
