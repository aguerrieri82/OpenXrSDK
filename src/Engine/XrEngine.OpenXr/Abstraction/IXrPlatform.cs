using Microsoft.Extensions.Logging;
using OpenXr.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.OpenXr
{
    public interface IXrPlatform
    {
        XrApp CreateXrApp(IXrGraphicDriver xrDriver);

        void CreateDrivers(XrEngineAppOptions options, out IRenderEngine renderEngine, out IXrGraphicDriver xrDriver);

        public IAssetManager AssetManager { get; }

        public ILogger Logger { get; }
    }
}
