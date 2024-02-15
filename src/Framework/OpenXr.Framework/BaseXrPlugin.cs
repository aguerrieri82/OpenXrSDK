using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public abstract class BaseXrPlugin : IXrPlugin
    {
        protected XrApp? _app;

        public BaseXrPlugin()
        {

        }

        public virtual void ConfigureSwapchain(ref SwapchainCreateInfo info)
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

        public virtual void SelectRenderOptions(XrViewInfo viewInfo, XrRenderOptions result)
        {

        }

        public XrApp App => _app ?? throw new ArgumentNullException();
    }
}
