using System.Numerics;
using XrMath;
using static XrEngine.Bullet.BulletLib;

namespace XrEngine.Bullet
{

    public class IkSolver
    {
        readonly Dictionary<IkNode, uint> _nodeMap = [];
        readonly Dictionary<IkNode, uint> _targetMap = [];
        IkContext _ctx;
        IkNode? _root;

        public IkSolver()
        {
            WorldPose = Pose3.Identity;
        }

        public void Build(IkNode root)
        {
            uint ix = 0;
            uint targetIx = 0;

            List<IkNode> nodes = new List<IkNode>();

            void Collect(IkNode node)
            {
                nodes.Add(node);

                if (node.Left != null)
                    Collect(node.Left);

                if (node.Right != null)
                    Collect(node.Right);
            }

            void Link(IkNode node)
            {
                if (node.Left != null)
                {
                    _ctx.IkInsertLeftChild(_nodeMap[node], _nodeMap[node.Left]);
                    node.Left.Parent = node;
                    Link(node.Left);
                }

                if (node.Right != null)
                {
                    _ctx.IkInsertRightSibling(_nodeMap[node], _nodeMap[node.Right]);
                    node.Right.Parent = node.Parent;
                    Link(node.Right);
                }
            }

            Collect(root);

            int targetsCount = nodes.Where(a => a.Purpose == Purpose.Effector).Count();

            _ctx = IkCreate((uint)nodes.Count, (uint)targetsCount);

            foreach (IkNode node in nodes)
            {
                _nodeMap[node] = ix;
                _ctx.IkCreateNode(ix, node.Attach, node.Axis, node.Size, node.Purpose, node.MinTheta, node.MaxTheta, node.RestAngle);
                ix++;

                if (node.Purpose == Purpose.Effector)
                {
                    _targetMap[node] = targetIx;
                    targetIx++;
                }
            }

            _ctx.IkInsertRoot(0);

            Link(root);

            _ctx.IkInit();

            _root = root;
        }

        public void Reset()
        {
            _ctx.IkReset();
        }

        public void Update(IkUpdateMethod method, bool updateTheta = true)
        {
            _ctx.IkUpdate(method, updateTheta);
            foreach (KeyValuePair<IkNode, uint> item in _nodeMap)
                item.Key.Theta = _ctx.IkGetNodeTheta(item.Value);
        }

        public void SetTarget(IkNode node, Vector3 pos)
        {
            uint ix = _targetMap[node];

            Vector3 localPos = WorldPose.Inverse().Transform(pos);

            _ctx.IkSetTarget(ix, localPos);
        }

        public IkNode? FindNode(string name)
        {
            return _nodeMap.Keys.FirstOrDefault(a => a.Name == name);
        }

        public IkNode? Root => _root;

        public IEnumerable<IkNode> Effectors => _targetMap.Keys;

        public IEnumerable<IkNode> Nodes => _nodeMap.Keys;

        public Pose3 WorldPose { get; set; }
    }
}
