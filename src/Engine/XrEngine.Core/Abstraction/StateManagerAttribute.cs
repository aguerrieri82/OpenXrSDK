using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public enum StateManagerMode
    {
        Explicit,
        Auto
    }

    [AttributeUsage(AttributeTargets.Class)]    
    public class StateManagerAttribute : Attribute
    {
        public StateManagerAttribute(StateManagerMode mode)
        {
            Mode = mode;
        }

        public StateManagerMode Mode { get; }
    }
}
