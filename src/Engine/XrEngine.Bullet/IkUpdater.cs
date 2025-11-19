using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using XrMath;
using static XrEngine.Bullet.BulletLib;

namespace XrEngine.Bullet
{
    public class IkUpdater : Behavior<Object3D>
    {
        Dictionary<IkNode, Object3D> _targets = [];


        public IkUpdater()
        {
            Method = IkUpdateMethod.IK_DLS;
        }

        protected override void Update(RenderContext ctx)
        {
            if (Solver?.Root == null)
                return;

            int i = 0;

            foreach (var effector in Solver.Effectors)
            {
                if (_targets.TryGetValue(effector, out var obj))
                    Solver.SetTarget(effector, obj.WorldPosition);
                i++;
            }

            Solver.Update(Method, true);

        }

        public void SetTarget(string name, Object3D obj)
        {
            var effector = Solver?.Effectors.First(a => a.Name == name);
            if (effector != null)
                SetTarget(effector, obj);
        }

        public void SetTarget(IkNode effector, Object3D obj)
        {
            _targets[effector] = obj;   
        }


        [Action]
        public void Reset()
        {
            Solver?.Reset();
        }

        public IkUpdateMethod Method { get; set; }

        public IkSolver? Solver { get; set; }
    }
}
