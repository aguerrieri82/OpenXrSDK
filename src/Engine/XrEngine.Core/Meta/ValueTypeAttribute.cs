using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public enum ValueType
    {
        None,
        Radiant
    }

    [AttributeUsage(AttributeTargets.Property)]    
    public class ValueTypeAttribute : Attribute
    {
        public ValueTypeAttribute(ValueType type)
        {
            Type = type;
        }

        public ValueType Type { get; }
    }
}
