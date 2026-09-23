using System.Numerics;
using XrMath;

namespace XrEngine.IK
{
    public class IkUpdater : Behavior<Object3D>
    {
        readonly Dictionary<int, IWorldLocatable> _targets = [];
        IkTarget[] _solveTargets = [];

        protected override void Update(RenderContext ctx)
        {
            if (Solver == null || Body == null || _targets.Count == 0)
                return;

            if (_solveTargets.Length < _targets.Count)
                _solveTargets = new IkTarget[_targets.Count];

            var invWorldPose = WorldPose.Inverse();
            var i = 0;

            foreach (var item in _targets)
            {
                _solveTargets[i++] = new IkTarget(
                    item.Key,
                    invWorldPose.Transform(item.Value.WorldPosition));
            }

            Solver.Solve(_solveTargets.AsSpan(0, i));
        }

        public void SetTarget(string name, IWorldLocatable obj)
        {
            if (Body == null)
                return;

            SetTarget(Body[name], obj);
        }

        public void SetTarget(int bone, IWorldLocatable obj)
        {
            _targets[bone] = obj;
        }

        public void RemoveTarget(string name)
        {
            if (Body != null && Body.TryGetBone(name, out var bone))
                _targets.Remove(bone);
        }

        public void RemoveTarget(int bone)
        {
            _targets.Remove(bone);
        }

        public void ClearTargets()
        {
            _targets.Clear();
        }

        [Action]
        public void Reset()
        {
            Solver?.Reset();
        }

        public IkBodies.Body? Body { get; set; }

        public IkSolver? Solver { get; set; }

        public Pose3 WorldPose { get; set; } = Pose3.Identity;
    }
}