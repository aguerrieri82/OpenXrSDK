using OpenXr.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.OpenXr
{
    public class XrInputStateManager : ITypeStateManager<IXrInput>
    {
        public IXrInput Read(string key, Type objType, IStateContainer container, StateContext ctx)
        {
            var name = container.Read<string>(key);
            return XrApp.Current!.Inputs[name];
        }

        public void Write(string key, IXrInput obj, IStateContainer container, StateContext ctx)
        {
            container.Write(key, obj.Name);
        }
    }
}
