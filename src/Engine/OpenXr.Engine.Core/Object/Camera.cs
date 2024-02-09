using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public abstract class Camera : Object3D
    {
        public Camera()
        {
            Near = 0.01f;
            Far = 10;
        }

        public float Near { get; set; }

        public float Far { get; set; }

        public Matrix4x4 Projection { get; set; }
    }
}
