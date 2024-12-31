using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrEngine;
using static XrSamples.Earth.SceneConst;

namespace XrSamples.Earth
{
    public class StarDome : TriangleMesh
    {
        public StarDome()
        {
            Size = 1;
            Geometry = new Sphere3D(1, 500);
            Materials.Add(new StarDomeMaterial());

            _transform.Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)DegreesToRadians(-23.5f)) *
                                     Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI);
        }


        public float Size
        {
            get => _transform.Scale.X / AU;

            set => _transform.SetScale(value * AU);
        }
    }
}
