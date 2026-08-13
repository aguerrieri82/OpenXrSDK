using System.Numerics;
using XrMath;

namespace XrEngine
{
    public struct SkinData
    {
        [ShaderRef(5, "aJointIndices", VertexComponent.JointIndex, IsIntegerStore = true)]
        public Vector4I JointIndices;


        [ShaderRef(6, "aJointWeights", VertexComponent.JointWeight)]
        public Vector4 JointWeights;
    }
}
