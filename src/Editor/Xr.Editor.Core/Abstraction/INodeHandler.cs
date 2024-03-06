using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Editor
{
    public interface INodeHandler
    {
        bool CanHandle(object value);

        INode CreateNode(object value); 
    }
}
