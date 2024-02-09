using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public abstract class Light : Object3D
    {
        public Light()
        {
            Color = Color.White;
            Intensity = 1f;
        }

        public Color Color { get; set; }

        public float Intensity { get; set; }    
    }
}
