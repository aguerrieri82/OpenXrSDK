using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Xr.Engine.OpenXr
{
    public class OculusHandView : Group
    {
        protected XrHandInputMesh _input;
        protected bool _isInit;

        public OculusHandView(XrHandInputMesh input) 
        {
            _input = input;

        }

        protected override void UpdateSelf(RenderContext ctx)
        {
        
            if (!_isInit && XrApp.Current != null && XrApp.Current.IsStarted)
            {
                _input.LoadMesh();

                var capMaterial = PbrMaterial.CreateDefault();
                capMaterial.Color = new Color(150 / 255f, 79 / 255f, 72 / 255f);
                capMaterial.DoubleSided = false;

                foreach (var capsule in _input.Mesh!.Capsules!)
                {
                    var dir = (capsule.Points.Element1.ToVector3() - capsule.Points.Element0.ToVector3());

                    var len = dir.Length();

                    var capMesh = new TriangleMesh(new Capsule(capsule.Radius, len), capMaterial);

                    AddChild(capMesh);
                }
                _isInit = true;
            }

            if (_isInit && _input.IsActive)
            {
                for (var i = 0; i < _input.Capsules.Length; i++)
                {
                    var capsule = _input.Capsules[i];

                    var mesh = Children[i];

                    var p0 = capsule.Points.Element0.ToVector3();
                    var p1 = capsule.Points.Element1.ToVector3();

                    var dir = (p1 - p0);

                    var h = dir.Length() / _input.Scale;

                    var orientation = MathUtils.QuatFromForwardUp(dir.Normalize(), new Vector3(0, 1, 0));

                    var start = p0 + Vector3.Transform(new Vector3(0, 0, -h / 2), orientation);

                    mesh.Transform.Position = start;
                    mesh.Transform.Orientation = orientation;
                    mesh.Transform.SetScale(_input.Scale);
                }
            }

            base.UpdateSelf(ctx);
        }
    }
}
