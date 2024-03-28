using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEditor.Nodes
{
    public class NodeDictionary : KeyedCollection<object, INode>
    {
        protected override object GetKeyForItem(INode item)
        {
            return item.Value;
        }
    }
}
