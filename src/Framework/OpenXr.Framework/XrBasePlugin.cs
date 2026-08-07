using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public abstract class XrBasePlugin : IXrPlugin
    {
        protected XrApp? _app;

        public XrBasePlugin()
        {

        }

        public virtual void ConfigureSwapchain(ref SwapchainCreateInfo info, bool mainSwapChain)
        {
        }

        public virtual void HandleEvent(ref EventDataBuffer buffer)
        {

        }

        public virtual void Initialize(XrApp app, IList<string> extensions)
        {

        }

        public virtual void OnInstanceCreated()
        {

        }

        public virtual void OnSessionBegin()
        {

        }

        public virtual void OnSessionCreated()
        {

        }

        public virtual void OnSessionEnd()
        {

        }

        public virtual void OnFrameEnd()
        {

        }

        public virtual void SelectRenderOptions(XrViewInfo viewInfo, XrRenderOptions result)
        {

        }

        public virtual IDisposable? Configure<T>(ref T data) where T : struct
        {
            return null;
        }

        public virtual void CreateInstance(ref InstanceCreateInfo info)
        {
        }

        public XrApp App => _app ?? throw new ArgumentNullException();
    }
}
