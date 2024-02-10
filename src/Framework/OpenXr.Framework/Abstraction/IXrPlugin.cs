using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public interface IXrPlugin
    {
        void ConfigureSwapchain(ref SwapchainCreateInfo info);

        void Initialize(XrApp app, IList<string> extensions);

        void HandleEvent(ref EventDataBuffer buffer);

        void OnInstanceCreated();

        void OnSessionCreated();

        void OnSessionBegin();

        void OnSessionEnd();
    }
}
