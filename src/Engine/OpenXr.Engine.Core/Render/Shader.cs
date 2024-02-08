using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class Shader : EngineObject
    {

        public string? VertexSource { get; set; }

        public string? FragmentSource { get; set; }
    }
}
