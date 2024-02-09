using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class PerspectiveCamera : Camera    
    { 
        public void SetFovCenter(float left, float right, float top, float bottom)
        {
            Projection = Matrix4x4.CreatePerspectiveOffCenter(left, right, bottom, top, Near, Far);
        }


        public void SetFov(float angleDegree, uint width, uint height)
        {
            SetFov(angleDegree, (float)width / height);
        }

        public void SetFov(float angleDegree, float ratio)
        {
            var rads = MathF.PI / 180f * angleDegree;

            Projection = Matrix4x4.CreatePerspectiveFieldOfView(rads, ratio, Near, Far);
        }

        public void LookAt(Vector3 position, Vector3 target, Vector3 up)
        {
            Transform.SetMatrix(Matrix4x4.CreateLookAt(position, target, up));
        }

    }
}
