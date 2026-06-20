using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine
{
    [AttributeUsage(AttributeTargets.Property)]
    public class NotifyAttribute : Attribute
    {
        public NotifyAttribute(ChangeType type)
        {
            Type = type;
        }

        public ChangeType Type { get; }
    }
}
