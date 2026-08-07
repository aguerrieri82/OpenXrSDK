using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public interface IXrPlugin
    {
        void SelectRenderOptions(XrViewInfo viewInfo, XrRenderOptions result);

        void ConfigureSwapchain(ref SwapchainCreateInfo info, bool mainSwapChain);

        void Initialize(XrApp app, IList<string> extensions);

        void HandleEvent(ref EventDataBuffer buffer);

        void OnInstanceCreated();

        void OnSessionCreated();

        void OnSessionBegin();

        void OnSessionEnd();

        void CreateInstance(ref InstanceCreateInfo info);

        IDisposable? Configure<T>(ref T data) where T : struct;
    }
}
