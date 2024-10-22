using Silk.NET.Core;
using Silk.NET.OpenXR;
using System.Reflection;
using System.Runtime.InteropServices;

#pragma warning disable CS8618 

namespace OpenXr.Framework.Oculus
{
    public class METAEnvironmentDepth
    {
        public METAEnvironmentDepth(XR xr, Instance instance)
        {
            var fun = new PfnVoidFunction();

            foreach (var prop in GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var propType = prop.FieldType;
                if (!propType.IsSubclassOf(typeof(Delegate)))
                    continue;

                var name = "xr" + prop.Name;

                var res = xr.GetInstanceProcAddr(instance, name, ref fun);
                if (res != Result.Success)
                    throw new NotSupportedException(name);

                prop.SetValue(this, Marshal.GetDelegateForFunctionPointer(fun, propType));
            }
        }

        public CreateEnvironmentDepthProviderMETADelegate CreateEnvironmentDepthProviderMETA;
        public DestroyEnvironmentDepthProviderMETADelegate DestroyEnvironmentDepthProviderMETA;
        public StartEnvironmentDepthProviderMETADelegate StartEnvironmentDepthProviderMETA;
        public StopEnvironmentDepthProviderMETADelegate StopEnvironmentDepthProviderMETA;
        public AcquireEnvironmentDepthImageMETADelegate AcquireEnvironmentDepthImageMETA;
        public CreateEnvironmentDepthSwapchainMETADelegate CreateEnvironmentDepthSwapchainMETA;
        public DestroyEnvironmentDepthSwapchainMETADelegate DestroyEnvironmentDepthSwapchainMETA;
        public EnumerateEnvironmentDepthSwapchainImagesMETADelegate EnumerateEnvironmentDepthSwapchainImagesMETA;
        public GetEnvironmentDepthSwapchainStateMETADelegate GetEnvironmentDepthSwapchainStateMETA;
        public SetEnvironmentDepthHandRemovalMETADelegate SetEnvironmentDepthHandRemovalMETA;


        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result CreateEnvironmentDepthProviderMETADelegate(
            Session session,
            ref EnvironmentDepthProviderCreateInfoMETA createInfo,
            out EnvironmentDepthProviderMETA environmentDepthProvider);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result DestroyEnvironmentDepthProviderMETADelegate(
           EnvironmentDepthProviderMETA environmentDepthProvider);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result StartEnvironmentDepthProviderMETADelegate(
                EnvironmentDepthProviderMETA environmentDepthProvider);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result StopEnvironmentDepthProviderMETADelegate(
                EnvironmentDepthProviderMETA environmentDepthProvider);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result AcquireEnvironmentDepthImageMETADelegate(
            EnvironmentDepthProviderMETA environmentDepthProvider,
            ref EnvironmentDepthImageAcquireInfoMETA acquireInfo,
            out EnvironmentDepthImageMETA environmentDepthImage);


        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result CreateEnvironmentDepthSwapchainMETADelegate(
            EnvironmentDepthProviderMETA environmentDepthProvider,
            ref EnvironmentDepthSwapchainCreateInfoMETA createInfo,
            out EnvironmentDepthSwapchainMETA swapchain);


        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result DestroyEnvironmentDepthSwapchainMETADelegate(
            EnvironmentDepthSwapchainMETA swapchain);


        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public unsafe delegate Result EnumerateEnvironmentDepthSwapchainImagesMETADelegate(
            EnvironmentDepthSwapchainMETA swapchain,
            uint imageCapacityInput,
            out uint imageCountOutput,
            SwapchainImageBaseHeader* images);


        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public unsafe delegate Result GetEnvironmentDepthSwapchainStateMETADelegate(
                EnvironmentDepthSwapchainMETA swapchain,
                out EnvironmentDepthSwapchainStateMETA state);


        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public unsafe delegate Result SetEnvironmentDepthHandRemovalMETADelegate(
            EnvironmentDepthProviderMETA environmentDepthProvider,
            ref EnvironmentDepthHandRemovalSetInfoMETA setInfo);


        public const string ExtensionName = "XR_META_environment_depth";
    }
}
