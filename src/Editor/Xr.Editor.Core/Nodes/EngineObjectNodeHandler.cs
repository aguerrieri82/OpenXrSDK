using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xr.Engine;

namespace Xr.Editor.Nodes
{
    public class EngineObjectNodeHandler : INodeHandler
    {
        public bool CanHandle(object value)
        {
            return value is EngineObject;
        }

        public INode CreateNode(object value)
        {
            if (value is Object3D obj)
            {
                var node = obj.GetProp<INode>("Node");
                if (node == null)
                {
                    if (obj is Group3D grp)
                        node = new GroupNode(grp);
                }

                if (node != null)
                {
                    obj.SetProp("Node", node);
                    return node;
                }
            }


            throw new NotSupportedException();
        }
    }
}
