using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine.Components
{
    public class DebugGizmos : Behavior<Scene>, IDrawGizmos
    {
        public void DrawGizmos(Canvas3D canvas)
        {
            canvas.Save();

            canvas.State.Color = new Color(1, 1, 0, 1);

            foreach (var obj in _host!.DescendantsWithFeature<Geometry3D>())
            {
                var local = obj.Feature!.Bounds;
                canvas.State.Transform = obj.Object.WorldMatrix;
                canvas.DrawBounds(local);
            }
    

            canvas.Restore();
        }
    }
}
