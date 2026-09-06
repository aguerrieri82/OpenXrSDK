using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public interface IXrPlugin
    {
        void SelectRenderOptions(XrViewInfo viewInfo, XrRenderOptions result);

        void Initialize(XrApp app, IList<string> extensions);

        void HandleEvent(ref EventDataBuffer buffer);

        void OnInstanceCreated();

        void OnSessionCreated();

        void OnSessionBegin();

        void OnSessionEnd();

        void Configure(ref SwapchainCreateInfo info, SwapchainTarget target);

        IDisposable? Configure<T>(ref T data) where T : unmanaged;
    }
}
