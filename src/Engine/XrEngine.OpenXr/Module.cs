using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine;

[assembly: Module(typeof(XrEngine.OpenXr.Module))]

namespace XrEngine.OpenXr
{
    public class Module : IModule
    {
        public void Load()
        {
            TypeStateManager.Instance.Register(new XrInputStateManager());
        }
    }
}

