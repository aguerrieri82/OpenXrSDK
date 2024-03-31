using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ModuleAttribute : Attribute
    {
        public ModuleAttribute(Type moduleType)
        {
            ModuleType = moduleType;    
        }

        public Type ModuleType { get; }
    }

    public interface IModule
    {
        void Load();
    }
}
