using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine.Physics
{
    public class JointConnection : BaseComponent<Object3D>, IDisposable, IDrawGizmos
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

        public void DrawGizmos(Canvas3D canvas)
        {
            if (!Joint.IsCreated)
                return;

            canvas.Save();

            canvas.State.Transform = Joint.Object0!.WorldMatrix;
            canvas.State.Color = "#ff0000"; 

            var ps0 = Joint.BaseJoint.LocalPose0;

            var start = ps0.Position;
            var end = start + Vector3.UnitX.Transform(ps0.Orientation) * 0.5f;

            canvas.DrawLine(start, end);

            canvas.State.Transform = Joint.Object1!.WorldMatrix;
            canvas.State.Color = "#00FF00";

            var ps1 = Joint.BaseJoint.LocalPose1;
            start = ps1.Position;
            end = start + Vector3.UnitX.Transform(ps1.Orientation) * 0.5f;
            canvas.DrawLine(start, end);

            canvas.Restore();
        }

        public Object3D? Other => Index == 0 ? Joint.Object1 : Joint.Object0;

        public Joint Joint { get; }

        public int Index { get; }
    }
}
