using System.Numerics;
using XrMath;

namespace XrEngine.IK
{
    public class IkViewer : Behavior<Group3D>, IDrawGizmos
    {
        readonly Dictionary<int, TriangleMesh> _meshMap = [];

        IkBodies.Body? _body;
        bool _isMeshCreated;

        public IkViewer()
        {
            EnableGizmos = true;
        }

        protected override void Update(RenderContext ctx)
        {
            if (Solver == null || Body == null)
                return;

            if (!_isMeshCreated)
                CreateMesh();

            UpdateMesh();
        }

        void CreateMesh()
        {
            if (_host == null || Solver == null || Body == null)
                return;

            _host.Clear();
            _meshMap.Clear();

            for (var i = 0; i < Body.Bones.Length; i++)
            {
                if (Body.Bones[i].Parent < 0)
                    continue;

                var mat = MaterialFactory.CreatePbr(new Color(1f, 1, 0, 0.8f));
                mat.Alpha = AlphaMode.Blend;

                var mesh = new TriangleMesh(Cube3D.Default, (Material)mat)
                {
                    Name = Body.Names[i]
                };

                mesh.SetProp("IkBone", i);

                _meshMap[i] = mesh;
                _host.AddChild(mesh);
            }

            _isMeshCreated = true;
        }

        void UpdateMesh()
        {
            if (Solver == null || Body == null)
                return;

            var poses = Solver.Poses;

            foreach (var item in _meshMap)
            {
                var index = item.Key;
                var mesh = item.Value;

                ref readonly var bone = ref Body.Bones[index];

                if (bone.Parent < 0)
                    continue;

                var p0 = poses[bone.Parent].Start;
                var p1 = poses[index].Start;

                var delta = p1 - p0;
                var len = delta.Length();

                if (len <= 1e-6f)
                {
                    mesh.Transform.Scale = Vector3.Zero;
                    continue;
                }

                var axis = delta / len;
                var size = Body.Sizes[index] * 0.3f;

                mesh.Transform.Position = p0;
                mesh.Transform.Scale = new Vector3(size, size, len);
                mesh.Transform.LocalPivot = new Vector3(0, 0, -0.5f);
                mesh.Transform.Orientation = Vector3.UnitZ.RotationTowards(axis);
            }
        }

        public void DrawGizmos(Canvas3D canvas, RenderContext ctx)
        {
            if (Solver == null || Body == null || !EnableGizmos || _host == null)
                return;

            var poses = Solver.Poses;
            var world = _host.WorldMatrix;

            for (var i = 0; i < Body.Bones.Length; i++)
            {
                ref readonly var bone = ref Body.Bones[i];
                ref readonly var pose = ref poses[i];

                var pos = Vector3.Transform(pose.Start, world);

                var rotation = Matrix4x4.CreateFromQuaternion(pose.Tip.Orientation) * world;
                var bx = Vector3.TransformNormal(Vector3.UnitX, rotation).Normalize();
                var by = Vector3.TransformNormal(Vector3.UnitY, rotation).Normalize();
                var bz = Vector3.TransformNormal(Vector3.UnitZ, rotation).Normalize();

                canvas.State.Color = new Color(1, 0, 0, 1);
                canvas.DrawLine(pos, pos + bx * 0.05f);

                canvas.State.Color = new Color(0, 1, 0, 1);
                canvas.DrawLine(pos, pos + by * 0.05f);

                canvas.State.Color = new Color(0, 0, 1, 1);
                canvas.DrawLine(pos, pos + bz * 0.05f);

                if ((bone.Axes & IkAxis.X) != 0)
                    DrawAxis(canvas, pos, bx);

                if ((bone.Axes & IkAxis.Y) != 0)
                    DrawAxis(canvas, pos, by);

                if ((bone.Axes & IkAxis.Z) != 0)
                    DrawAxis(canvas, pos, bz);

                if (bone.Parent >= 0)
                {
                    var parentPos = Vector3.Transform(poses[bone.Parent].Start, world);

                    canvas.State.Color = new Color(1, 0, 1, 1);
                    canvas.DrawLine(parentPos, pos);
                }
            }
        }

        static void DrawAxis(Canvas3D canvas, Vector3 pos, Vector3 axis)
        {
            canvas.State.Color = new Color(1, 1, 0, 1);

            canvas.DrawCircle(new Pose3
            {
                Position = pos,
                Orientation = Vector3.UnitZ.RotationTowards(axis)
            }, 0.03f);
        }

        public IkBodies.Body? Body
        {
            get => _body;
            set
            {
                if (ReferenceEquals(_body, value))
                    return;

                _body = value;
                _isMeshCreated = false;
            }
        }

        public IkSolver? Solver { get; set; }

        public bool EnableGizmos { get; set; }
    }
}