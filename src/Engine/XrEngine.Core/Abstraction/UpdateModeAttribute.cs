using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine
{
    [AttributeUsage(AttributeTargets.Class)]
    public class UpdateModeAttribute : Attribute
    {

        public bool IsParallel { get; set; }
    }
}
