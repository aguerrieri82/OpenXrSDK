using System.Numerics;
using XrMath;

namespace XrEngine.Physics
{
    public class JointConnection : BaseComponent<Object3D>, IDisposable, IDrawGizmos
    {
        private PhysicsManager? _manager;

        public JointConnection(Joint joint, int index)
        {
            Joint = joint;
            Index = index;
        }

        public void Dispose()
        {
            var other = Other?.Components<JointConnection>().Where(a => a.Joint == Joint).FirstOrDefault();

            if (other != null)
                Other?.RemoveComponent(other);

            Joint.Dispose();
        }


        public void DrawGizmos(Canvas3D canvas)
        {
            if (!Joint.IsCreated)
                return;

            _manager ??= _host!.Scene!.Component<PhysicsManager>();

            if ((_manager.DebugGizmos & PhysicsDebugGizmos.Joints) == 0)
                return;

            canvas.Save();

            var ps0 = Joint.BaseJoint.LocalPose0;
            var ps1 = Joint.BaseJoint.LocalPose1;

            ps0.Position -= -Joint.Object0.Transform.LocalPivot;
            ps1.Position -= -Joint.Object1.Transform.LocalPivot;


            var start = ps0.Position;
            var end = start + Vector3.UnitX.Transform(ps0.Orientation) * 0.5f;
            canvas.State.Transform = Joint.Object0!.WorldMatrix;
            canvas.State.Color = "#ff0000";
            canvas.DrawLine(start, end);

            start = ps1.Position;
            end = start + Vector3.UnitX.Transform(ps1.Orientation) * 0.5f;
            canvas.State.Transform = Joint.Object1!.WorldMatrix;
            canvas.State.Color = "#800000";
            canvas.DrawLine(start, end);

            //Y
            start = ps0.Position;
            end = start + Vector3.UnitY.Transform(ps0.Orientation) * 0.5f;
            canvas.State.Transform = Joint.Object0!.WorldMatrix;
            canvas.State.Color = "#00ff00";
            canvas.DrawLine(start, end);

            start = ps1.Position;
            end = start + Vector3.UnitY.Transform(ps1.Orientation) * 0.5f;
            canvas.State.Transform = Joint.Object1!.WorldMatrix;
            canvas.State.Color = "#008000";
            canvas.DrawLine(start, end);

            //Z
            start = ps0.Position;
            end = start + Vector3.UnitZ.Transform(ps0.Orientation) * 0.5f;
            canvas.State.Transform = Joint.Object0!.WorldMatrix;
            canvas.State.Color = "#0000ff";
            canvas.DrawLine(start, end);

            start = ps1.Position;
            end = start + Vector3.UnitZ.Transform(ps1.Orientation) * 0.5f;
            canvas.State.Transform = Joint.Object1!.WorldMatrix;
            canvas.State.Color = "#000080";
            canvas.DrawLine(start, end);

            canvas.Restore();
        }

        public Object3D? Other => Index == 0 ? Joint.Object1 : Joint.Object0;

        public Joint Joint { get; }

        public int Index { get; }
    }
}
