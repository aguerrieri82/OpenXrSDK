using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Text;
using XrMath;

namespace XrEngine.Bullet
{
    public class IkViewer : Behavior<Group3D>, IDrawGizmos
    {
        Dictionary<IkNode, TriangleMesh> _meshMap = [];
        bool _isMeshCreated;

        public IkViewer()
        {
            EnableGizmos = true;
        }


        protected override void Update(RenderContext ctx)
        {
            if (Solver?.Root == null)
                return;

            if (!_isMeshCreated)
                CreateMesh();

            var wordTransform = Solver.WorldPose.ToMatrix();

            //UpdateMesh(Solver.Root, Solver.Root.GetLocalTransform() * wordTransform, wordTransform);

            //TODO: we should take wordTransform and multiply the host word transform inverse,
            //now we assume that Solver.WorldPose = _host.WordPose;

            UpdateMesh(Solver.Root, Solver.Root.GetLocalTransform(), Matrix4x4.Identity);
        }


        void CreateMesh()
        {
            if (_host == null || Solver == null)
                return;

            TriangleMesh Create(IkNode node)
            {
                var mat = MaterialFactory.CreatePbr(new Color(1f, 1, 0, 0.8f));
                mat.Alpha = AlphaMode.Blend;

                var box = new TriangleMesh(Cube3D.Default, (Material)mat)
                {
                    Name = node.Name
                };

                box.SetProp("IkNode", node);

                _meshMap[node] = box;

                _host.AddChild(box);

                return box;
            }

            _host.Clear();

            foreach (var node in Solver.Nodes)
            {
                if (node.Parent == null)
                    continue;

                Create(node);
            }

            _isMeshCreated = true;
        }

        void UpdateMesh(IkNode node, Matrix4x4 baseTransform, Matrix4x4 parentTransform)
        {
            void Update(IkNode newNode, Matrix4x4 newBase, Vector3 p0)
            {
                var mesh = _meshMap[newNode];

                var p1 = newBase.Translation;

                var len = (p1 - p0).Length();

                var axis = (p1 - p0).Normalize();

                var size = node.Size * 0.3f;

                mesh.Transform.Position = p0;
                mesh.Transform.Scale = new Vector3(size, size, len);
                mesh.Transform.LocalPivot = new Vector3(0, 0, -0.5f);
                mesh.Transform.Orientation = (Vector3.UnitZ).RotationTowards(axis);

            }

            if (node.Right != null)
            {
                var trSibling = node.Right.GetLocalTransform() * parentTransform;

                Update(node.Right, trSibling, parentTransform.Translation);

                UpdateMesh(node.Right, trSibling, parentTransform);
            }

            if (node.Left != null)
            {
                var trChild = node.Left.GetLocalTransform() * baseTransform;
                Update(node.Left, trChild, baseTransform.Translation);

                UpdateMesh(node.Left, trChild, baseTransform);
            }
        }

        void DrawWork(Canvas3D canvas, IkNode node, Matrix4x4 baseTransform, Matrix4x4 parentTransform)
        {
            if (node == null)
                return;

            var pos = baseTransform.Translation;

            var bx = new Vector3(baseTransform.M11, baseTransform.M12, baseTransform.M13);
            var by = new Vector3(baseTransform.M21, baseTransform.M22, baseTransform.M23);
            var bz = new Vector3(baseTransform.M31, baseTransform.M32, baseTransform.M33);

            canvas.State.Color = new Color(1, 0, 0, 1);  // X
            canvas.DrawLine(pos, pos + bx * 0.05f);

            canvas.State.Color = new Color(0, 1, 0, 1);  // Y
            canvas.DrawLine(pos, pos + by * 0.05f);

            canvas.State.Color = new Color(0, 0, 1, 1);  // Z
            canvas.DrawLine(pos, pos + bz * 0.05f);

            var axisWorld = Vector3.TransformNormal(node.Axis, baseTransform);

            canvas.State.Color = new Color(0.2f, 0.2f, 0.7f, 1);
            canvas.DrawLine(pos, pos + axisWorld * 0.1f);
            canvas.State.Color = new Color(1, 1, 0, 1);
            canvas.DrawCircle(new Pose3
            {
                Orientation = Vector3.UnitZ.RotationTowards(axisWorld),
                Position = pos,
            }, 0.03f);

            if (node.Right != null)
            {
                var trSibling = node.Right.GetLocalTransform() * parentTransform;

                canvas.State.Color = new Color(0, 1, 1, 1); // green
                canvas.DrawLine(parentTransform.Translation, trSibling.Translation);

                DrawWork(canvas, node.Right, trSibling, parentTransform);
            }

            if (node.Left != null)
            {
                var trChild = node.Left.GetLocalTransform() * baseTransform;

                canvas.State.Color = new Color(1, 0, 1, 1); // red
                canvas.DrawLine(pos, trChild.Translation);

                DrawWork(canvas, node.Left, trChild, baseTransform);
            }
        }

        public void DrawGizmos(Canvas3D canvas)
        {
            if (Solver?.Root == null || !EnableGizmos)
                return;
            
            var wordTransform = Solver.WorldPose.ToMatrix();

            DrawWork(canvas, Solver.Root, Solver.Root.GetLocalTransform() * wordTransform, wordTransform);
        }


        public IkSolver? Solver { get; set; }  
        
        public bool EnableGizmos { get; set; }  
    }
}
