using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public unsafe static class XrEngineNativeLib
    {

        public delegate void PxOnContactModifyCallback(void* pairs, uint count);


        [DllImport("xrengine-native")]
        public static extern void* CreatePxSimulationEventCallbackWrapper();


        [DllImport("xrengine-native")]
        public static extern void* CreatePxContactModifyCallbackWrapper(PxOnContactModifyCallback handler);
        
    }
}
