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
            
        }


        public float Intensity { get; set; }    
    }
}
