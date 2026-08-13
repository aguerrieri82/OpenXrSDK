using System.Numerics;
using XrMath;

namespace XrEngine
{
    public struct SkinData
    {
        [ShaderRef(5, "aJointIndices", VertexComponent.Skin, IsIntegerStore = true)]
        public Vector4I JointIndices;


        [ShaderRef(6, "aJointWeights", VertexComponent.Skin)]
        public Vector4 JointWeights;
    }
}
