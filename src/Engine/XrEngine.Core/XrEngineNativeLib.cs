using System.Runtime.InteropServices;

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
