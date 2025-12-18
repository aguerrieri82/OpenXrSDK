using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace XrEngine.Bullet
{
    
    public static class BulletLib
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct IkContext
        {
            public nint Handle;
        }

        public enum Purpose
        {
            Joint,
            Effector
        };

        public enum IkUpdateMethod
        {
            IK_JACOB_TRANS = 0,
            IK_PURE_PSEUDO,
            IK_DLS,
            IK_SDLS,
            IK_DLS_SVD
        };



        [DllImport("bullet-native")]
        public static extern IkContext IkCreate(uint nodeCount, uint targetCount);

        [DllImport("bullet-native")]
        public static extern void IkCreateNode(this IkContext ctx, uint index, Vector3 attach, Vector3 v, float size, Purpose purpose, float minTheta, float maxTheta, float restAngle);

        [DllImport("bullet-native")]
        public static extern void IkInsertLeftChild(this IkContext ctx, uint parentIndex, uint childIndex);

        [DllImport("bullet-native")]
        public static extern void IkInsertRightSibling(this IkContext ctx, uint parentIndex, uint childIndex);

        [DllImport("bullet-native")]
        public static extern void IkInsertRoot(this IkContext ctx, uint index);

        [DllImport("bullet-native")]
        public static extern float IkGetNodeTheta(this IkContext ctx, uint index);

        [DllImport("bullet-native")]
        public static extern void IkSetTarget(this IkContext ctx, uint index, Vector3 pos);

        [DllImport("bullet-native")]
        public static extern void IkUpdate(this IkContext ctx, IkUpdateMethod method, bool updateThetas);

        [DllImport("bullet-native")]
        public static extern void IkDelete(this IkContext ctx);

        [DllImport("bullet-native")]
        public static extern void IkInit(this IkContext ctx);

        [DllImport("bullet-native")]
        public static extern void IkReset(this IkContext ctx);
    }
}
