using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class AmbientLight : Light   
    {
        public AmbientLight()
        {

        }

        public AmbientLight(float intensity)
        {
            Intensity = intensity;  
        }
    }
}
