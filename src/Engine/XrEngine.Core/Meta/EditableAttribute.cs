using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine
{

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class EditableAttribute : Attribute
    {
    }
}
