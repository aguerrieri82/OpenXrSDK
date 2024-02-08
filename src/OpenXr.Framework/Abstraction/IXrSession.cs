using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework
{
    public interface IXrSession
    {
        bool IsStarted { get; }

        ulong SystemId { get; }

        Instance Instance { get; }

        Session Session { get; }

        XR Xr { get; }
    }
}
