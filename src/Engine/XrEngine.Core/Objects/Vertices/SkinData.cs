using System.Numerics;
using XrMath;

namespace XrEngine
{
    public struct SkinData
    {
        [ShaderRef(AttributeSlots.JointIndices, "aJointIndices", VertexComponent.JointIndex, IsIntegerStore = true)]
        public Vector4I JointIndices;

        [ShaderRef(AttributeSlots.JointWeights, "aJointWeights", VertexComponent.JointWeight)]
        public Vector4 JointWeights;
    }
}
