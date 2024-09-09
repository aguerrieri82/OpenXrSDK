﻿using XrEditor.Services;
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
                
                if (_value.MetallicRoughness?.BaseColorTexture != null)
                    yield return factory.CreateNode(_value.MetallicRoughness.BaseColorTexture);
                
                if (_value.MetallicRoughness?.MetallicRoughnessTexture != null)
                    yield return factory.CreateNode(_value.MetallicRoughness.MetallicRoughnessTexture);
                
                if (_value.NormalTexture != null)
                    yield return factory.CreateNode(_value.NormalTexture);

                if (_value.OcclusionTexture != null)
                    yield return factory.CreateNode(_value.OcclusionTexture);
            }
        }

    }
}
