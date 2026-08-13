using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace XrEngine
{
    public class Joint3D : Group3D
    {


        public Matrix4x4 InverseBindMatrix { get; set; }
    }
}
