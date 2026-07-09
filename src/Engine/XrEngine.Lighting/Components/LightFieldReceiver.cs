using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Lighting
{
    public class LightFieldReceiver : Behavior<TriangleMesh>
    {
        public LightFieldReceiver()
        {
            IsOccluder = true;  
        }

        public bool IsOccluder { get; set; }    
    }
}
