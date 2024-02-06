using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework
{
    public abstract class BaseXrPlugin : IXrPlugin
    {
        public virtual void HandleEvent(EventDataBuffer buffer)
        {

        }

        public virtual void Initialize(XrApp app, IList<string> extensions)
        {

        }

        public virtual void OnInstanceCreated()
        {

        }

        public virtual void OnSessionCreated()
        {

        }
    }
}
