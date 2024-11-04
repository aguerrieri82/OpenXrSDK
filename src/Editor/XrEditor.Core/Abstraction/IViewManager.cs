using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine;


namespace XrEditor
{
    public interface IViewManager
    {
        void AddView<T>(string path) where T : IModule;
    }
}
