using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Binding;
using XrEngine;

namespace XrEditor.Nodes
{
    public class ComponentNode<T> : BaseNode<T>, IEditorProperties, IItemView where T : IComponent
    {
        public ComponentNode(T value) : base(value)
        {
        }

        public void EditorProperties(IList<PropertyView> curProps)
        {
            var binder = new Binder<T>(_value);
            EditorProperties(binder, curProps);
        }

        protected virtual void EditorProperties(Binder<T> binder, IList<PropertyView> curProps)
        {

        }

        public virtual string DisplayName => _value.GetType().Name;

        public IconView? Icon => null;

        bool IEditorProperties.AutoGenerate { get; set; }
    }
}
