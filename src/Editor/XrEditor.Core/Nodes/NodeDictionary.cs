using System.Collections.ObjectModel;

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
