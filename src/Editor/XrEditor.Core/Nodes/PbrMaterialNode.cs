using UI.Binding;
using XrEditor.Services;
using XrEngine;


namespace XrEditor.Nodes
{
    public class PbrMaterialNode : MaterialNode<PbrV2Material>
    {
        public PbrMaterialNode(PbrV2Material value) : base(value)
        {
        }

        public override IEnumerable<INode> Children
        {
            get
            {
                var factory = Context.Require<NodeManager>();

                if (_value.ColorMap != null)
                    yield return factory.CreateNode(_value.ColorMap);

                if (_value.MetallicRoughnessMap != null)
                    yield return factory.CreateNode(_value.MetallicRoughnessMap);

                if (_value.NormalMap != null)
                    yield return factory.CreateNode(_value.NormalMap);

                if (_value.OcclusionMap != null)
                    yield return factory.CreateNode(_value.OcclusionMap);
            }
        }

        protected override void EditorProperties(Binder<PbrV2Material> binder, IList<PropertyView> curProps)
        {
            base.EditorProperties(binder, curProps);
        }

    }
}
