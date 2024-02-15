using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework
{
    public class XrPathAttribute : Attribute
    {
        public XrPathAttribute(string value) 
        {
            Value = value;
        }

        public string Value { get; }
    }
}
