using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
