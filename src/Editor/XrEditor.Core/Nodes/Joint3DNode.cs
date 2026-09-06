using XrEngine;

namespace XrEditor.Nodes
{
    public class Joint3DNode : Group3DNode<Joint3D>
    {
        public Joint3DNode(Joint3D value)
            : base(value)
        {
        }

        public override IconView? Icon => new()
        {
            Color = "#607D8B",
            Name = "icon_device_hub"
        };
    }
}
