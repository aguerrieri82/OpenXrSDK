using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{

    internal interface IHosted
    {
        void Attach(EngineObject obj);

        void Detach(EngineObject obj);

        IReadOnlySet<EngineObject> Hosts { get; }
    }
}
