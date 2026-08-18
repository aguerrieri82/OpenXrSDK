using System;
using System.Collections.Generic;
using System.Text;
using XrEditor.Services;
using XrEngine;
using XrEngine.Animation;

namespace XrEditor.Nodes
{
    public class AnimationHostNode : ComponentNode<AnimationsHost>
    {
        public AnimationHostNode(AnimationsHost value) 
            : base(value)
        {
        }

        public override IEnumerable<INode> Children
        {
            get
            {
                var factory = Context.Require<NodeManager>();

                foreach (var anim in _value.Animations)
                    yield return factory.CreateNode(anim, this);
            }
        }
    }
}
