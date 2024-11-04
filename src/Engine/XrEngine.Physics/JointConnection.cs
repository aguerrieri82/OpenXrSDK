using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine.Physics
{
    internal class JointConnection : BaseComponent<Object3D>, IDisposable
    {
        public JointConnection(Joint joint, int index)
        {
            Joint = joint;
            Index = index;  
        }

        public void Dispose()
        {
            var other = Other?.Components<JointConnection>().Where(a=> a.Joint == Joint).FirstOrDefault();
            
            if (other != null)
                Other?.RemoveComponent(other);

            Joint.Dispose();
        }

        [Range(0, 100, 0.1f)]
        public float Damping
        {
            get => Joint.Damping;
            set
            {
                Joint.Damping = value;  
                Joint.UpdatePhysics();
            }
        }

        [Range(0, 1000, 1f)]
        public float Stiffness
        {
            get => Joint.Stiffness;
            set
            {
                Joint.Stiffness = value;
                Joint.UpdatePhysics();
            }
        }

        public Object3D? Other => Index == 0 ? Joint.Object1 : Joint.Object0;

        public Joint Joint { get; }

        public int Index { get; }
    }
}
