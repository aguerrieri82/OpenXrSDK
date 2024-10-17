using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.OpenXr
{
    public class HitTargetView : TriangleMesh
    {
        public HitTargetView()
        {
            Flags |= EngineObjectFlags.DisableNotifyChangedScene;
            Name = "HitTargetView";

            var mat = new ColorMaterial("#00FF00A0");
            mat.WriteDepth = false;
            mat.UseDepth = false;
            mat.Alpha = AlphaMode.Blend;

            _materials.Add(mat);
            _geometry = new Donut3D(0.1f, 0.08f, 0.005f, 32);
        }
    }
}
