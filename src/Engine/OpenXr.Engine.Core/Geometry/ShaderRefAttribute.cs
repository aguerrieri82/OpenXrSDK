using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ShaderRefAttribute : Attribute
    {
        public ShaderRefAttribute(uint loc, string name)
        {
            Loc = loc;
            Name = name;
        }

        public uint Loc { get; }

        public string Name { get; }
    }
}
