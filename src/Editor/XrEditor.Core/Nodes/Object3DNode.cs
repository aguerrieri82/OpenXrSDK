﻿using UI.Binding;
using XrEditor.Services;
using XrEngine;

namespace XrEditor.Nodes
{
    public class Object3DNode<T> : EngineObjectNode<T> where T : Object3D
    {
        readonly NodeDictionary _components = [];

        public Object3DNode(T value)
            : base(value)
        {
            _components.Add(new Transform3DNode(value.Transform));
            _autoGenProps = true;
            value.Changed += OnElementChanged;
        }

        public override void Actions(IList<ActionView> result)
        {
            if (_value.Parent != null)
            {
                result.Add(new ActionView(() => _value.Parent.RemoveChild(_value))
                {
                    Icon = new IconView { Name = "icon_delete" },
                    DisplayName = "Remove"
                });
            }

            base.Actions(result);
        }

        protected virtual void OnElementChanged(EngineObject element, ObjectChange change)
        {

        }

        protected override void EditorProperties(Binder<T> binder, IList<PropertyView> curProps)
        {
            base.EditorProperties(binder, curProps);

            curProps.Add(new PropertyView
            {
                Label = "Visible",
                Editor = new BoolEditor(binder.Prop(a => a.IsVisible))
            });
        }

        protected INode GetNode(object value)
        {
            if (!_components.TryGetValue(value, out var node))
            {
                node = Context.Require<NodeManager>().CreateNode(value);
                _components.Add(node);
            }
            return node;
        }

        public override IEnumerable<INode> Components
        {
            get
            {
                yield return _components[Value.Transform];

                foreach (var component in _value.Components<IComponent>())
                    yield return GetNode(component);
            }
        }

        public override string DisplayName => _value.Name ?? _value.GetType().Name;

    }
}
