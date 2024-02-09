using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class PointLight : Light
    {
        public PointLight() 
        {
            Specular = Color.White;
        }

        public Color Specular { get; set; }
    }
}
