using UI.Binding;
using XrEngine;

namespace XrEditor.Nodes
{
    public class PlayerNode<T> : ComponentNode<T> where T : IComponent, IPlayer
    {

        public PlayerNode(T value) : base(value)
        {
            _autoGenProps = true;
        }

        protected override void EditorProperties(Binder<T> binder, IList<PropertyView> curProps)
        {
            curProps.Add(new PropertyView()
            {
                Editor = new PlayerView()
                {
                    EditValue = Value
                },
                Label = "Player",
                ReadOnly = true,
            });

            base.EditorProperties(binder, curProps);
        }

    }
}
