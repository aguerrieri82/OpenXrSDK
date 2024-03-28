using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine;

namespace XrEditor.Nodes
{
    public class ComponentNode<T> : BaseNode<T>, IEditorProperties, IItemView where T : IComponent
    {
        public ComponentNode(T value) : base(value)
        {
        }

        public virtual void EditorProperties(IList<PropertyView> curProps)
        {

        }

        public virtual string DisplayName => _value.GetType().Name;

        public IconView? Icon => null;


    }
}
