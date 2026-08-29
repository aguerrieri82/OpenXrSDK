using UI.Binding;
using XrEditor.Services;
using XrEngine;

namespace XrEditor.Nodes
{
    public class PbrMaterialNode : MaterialNode<PbrMaterial>
    {
        public PbrMaterialNode(PbrMaterial value) : base(value)
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

                if (_value.IridescenceMap != null)
                    yield return factory.CreateNode(_value.IridescenceMap);

                if (_value.IridescenceThicknessMap != null)
                    yield return factory.CreateNode(_value.IridescenceThicknessMap);

                if (_value.ThicknessMap != null)
                    yield return factory.CreateNode(_value.ThicknessMap);

                foreach (var child in base.Children)
                    yield return child;
            }
        }

        protected override void EditorProperties(Binder<PbrMaterial> binder, IList<PropertyView> curProps)
        {
            base.EditorProperties(binder, curProps);
        }

    }
}
