using System.Numerics;
using XrMath;
using static XrEngine.Bullet.BulletLib;

namespace XrEngine.Bullet
{
    public class IkSkeletonUpdater : Behavior<Joint3D>, IDrawGizmos
    {
        protected class JointBinding
        {
            public required Joint3D Joint;
            public IkNode? Node;
            public Quaternion ReferenceSolverOrientation;
            public Quaternion SolverDelta = Quaternion.Identity;
        }

        readonly Dictionary<Joint3D, JointBinding> _bindings = [];
        readonly Dictionary<IkNode, JointBinding> _nodeBindings = [];
        readonly Dictionary<IkNode, IWorldLocatable> _targets = [];

        Quaternion _referenceRootOrientation;

        bool _isBuilt;

        public IkSkeletonUpdater()
        {
            Method = IkUpdateMethod.IK_DLS;
            EnableGizmos = true;
        }

        protected override void Start(RenderContext ctx)
        {
            if (!_isBuilt)
                Build();
        }

        public void Build()
        {
            _bindings.Clear();
            _nodeBindings.Clear();
            _targets.Clear();

            _referenceRootOrientation = _host.Transform.Orientation;

            Solver = new IkSolver
            {
                WorldPose = _host.WorldMatrix.ToPose()
            };

            var solverInv = Solver.WorldPose.Inverse();
            var solverOrientationInv = Quaternion.Inverse(Solver.WorldPose.Orientation);

            Vector3 SolverPosition(Joint3D joint)
            {
                return solverInv.Transform(joint.WorldPosition);
            }

            Vector3 SolverAxis(Joint3D joint, Vector3 localAxis)
            {
                var worldAxis = Vector3.Transform(localAxis, joint.WorldOrientation);
                return Vector3.Normalize(Vector3.Transform(worldAxis, solverOrientationInv));
            }

            IkNode? BuildJoint(Joint3D joint)
            {
                var binding = new JointBinding
                {
                    Joint = joint,
                    ReferenceSolverOrientation =
                        solverOrientationInv * joint.WorldOrientation
                };

                _bindings[joint] = binding;

                var attach = SolverPosition(joint);
                var nodes = new List<IkNode>();

                void AddDof(JointDof? dof, Vector3 axis)
                {
                    if (dof == null || !dof.Enabled)
                        return;

                    nodes.Add(new IkNode
                    {
                        Name = joint.Name,
                        Attach = attach,
                        Axis = SolverAxis(joint, axis),
                        Purpose = Purpose.Joint,
                        MinTheta = dof.Min,
                        MaxTheta = dof.Max,
                        RestAngle = dof.Rest
                    });
                }

                AddDof(joint.DofX, Vector3.UnitX);
                AddDof(joint.DofY, Vector3.UnitY);
                AddDof(joint.DofZ, Vector3.UnitZ);

                if (joint.IsEffector)
                {
                    nodes.Add(new IkNode
                    {
                        Name = joint.Name,
                        Attach = attach,
                        Axis = Vector3.Zero,
                        Purpose = Purpose.Effector
                    });
                }

                IkNode? first = null;
                IkNode? last = null;

                foreach (var node in nodes)
                {
                    first ??= node;

                    last?.Child = node;

                    last = node;
                }

                if (last != null)
                {
                    binding.Node = last;
                    _nodeBindings[last] = binding;
                }

                IkNode? firstChild = null;
                IkNode? lastChild = null;

                foreach (var child in joint.Children.OfType<Joint3D>())
                {
                    var childNode = BuildJoint(child);

                    if (childNode == null)
                        continue;

                    if (firstChild == null)
                        firstChild = childNode;
                    else
                        lastChild!.Sibling = childNode;

                    lastChild = childNode;
                }

                if (first == null)
                    return firstChild;

                if (firstChild != null)
                    last!.Child = firstChild;

                return first;
            }

            var root = BuildJoint(_host);

            if (root != null)
                Solver.Build(root);

            _isBuilt = true;
        }

        static Quaternion NodeRotation(IkNode node)
        {
            if (node.Axis.LengthSquared() == 0)
                return Quaternion.Identity;

            return Quaternion.CreateFromAxisAngle(Vector3.Normalize(node.Axis), node.Theta);
        }

        void UpdateWorldPose()
        {
            var parentOrientation = _host.Parent?.WorldOrientation ?? Quaternion.Identity;

            Solver!.WorldPose = new Pose3
            {
                Position = _host.WorldPosition,
                Orientation = parentOrientation * _referenceRootOrientation
            };
        }

        protected override void Update(RenderContext ctx)
        {
            if (Solver?.Root == null)
                return;

            UpdateWorldPose();

            var solverInv = Solver.WorldPose.Inverse();

            foreach (var entry in _targets)
                Solver.SetTarget(entry.Key, entry.Value.WorldPosition);

            Solver.Update(Method, true);

            UpdateSolverTransforms(Solver.Root, NodeRotation(Solver.Root), Quaternion.Identity);

            ApplyJoints(_host, Quaternion.Identity, _host.Parent?.WorldOrientation ?? Quaternion.Identity);
        }

        void UpdateSolverTransforms(IkNode node, Quaternion baseRot, Quaternion parentRot)
        {
            if (_nodeBindings.TryGetValue(node, out var binding))
                binding.SolverDelta = baseRot;

            if (node.Sibling != null)
            {
                var siblingRotation = parentRot * NodeRotation(node.Sibling);
                UpdateSolverTransforms(node.Sibling, siblingRotation, parentRot);
            }

            if (node.Child != null)
            {
                var childRotation = baseRot * NodeRotation(node.Child);
                UpdateSolverTransforms(node.Child, childRotation, baseRot);
            }
        }

        void ApplyJoints(Joint3D joint, Quaternion parentSolverDelta, Quaternion parentWorldOrientation)
        {
            var binding = _bindings[joint];

            var solverDelta = binding.Node != null ? binding.SolverDelta : parentSolverDelta;

            var worldOrientation =
                Solver!.WorldPose.Orientation *
                solverDelta *
                binding.ReferenceSolverOrientation;

            joint.Transform.Orientation = Quaternion.Inverse(parentWorldOrientation) * worldOrientation;

            foreach (var child in joint.Children.OfType<Joint3D>())
                ApplyJoints(child, solverDelta, worldOrientation);
        }

        public void SetTarget(string name, IWorldLocatable obj)
        {
            if (!_isBuilt)
                Build();

            var effector = (Solver?.Effectors.FirstOrDefault(a => a.Name == name)) ??
                throw new InvalidOperationException($"Effector '{name}' not found");

            SetTarget(effector, obj);
        }

        public void SetTarget(IkNode effector, IWorldLocatable obj)
        {
            _targets[effector] = obj;
        }

        [Action]
        public void Reset()
        {
            Solver?.Reset();
        }

        static void DrawWork(Canvas3D canvas, IkNode node, Matrix4x4 baseTransform, Matrix4x4 parentTransform)
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

            if (node.Sibling != null)
            {
                var trSibling = node.Sibling.GetLocalTransform() * parentTransform;

                canvas.State.Color = new Color(0, 1, 1, 1); // green
                canvas.DrawLine(parentTransform.Translation, trSibling.Translation);

                DrawWork(canvas, node.Sibling, trSibling, parentTransform);
            }

            if (node.Child != null)
            {
                var trChild = node.Child.GetLocalTransform() * baseTransform;

                canvas.State.Color = new Color(1, 0, 1, 1); // red
                canvas.DrawLine(pos, trChild.Translation);

                DrawWork(canvas, node.Child, trChild, baseTransform);
            }
        }

        public void DrawGizmos(Canvas3D canvas, RenderContext ctx)
        {
            if (Solver?.Root == null || !EnableGizmos)
                return;

            var wordTransform = Solver.WorldPose.ToMatrix();

            DrawWork(canvas, Solver.Root, Solver.Root.GetLocalTransform() * wordTransform, wordTransform);
        }

        public bool EnableGizmos { get; set; }

        public IkUpdateMethod Method { get; set; }

        public IkSolver? Solver { get; private set; }
    }
}