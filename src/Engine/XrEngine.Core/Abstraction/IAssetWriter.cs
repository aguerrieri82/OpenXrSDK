using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public interface IAssetWriter
    {
        bool CanHandle(EngineObject obj);

        void SaveAsset(EngineObject obj, Stream stream);
    }
}
