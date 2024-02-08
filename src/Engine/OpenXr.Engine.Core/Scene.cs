using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class Scene : Group
    {
        public Scene()
        {
        }

        public Camera? MainCamera { get; set; }
    }
}
