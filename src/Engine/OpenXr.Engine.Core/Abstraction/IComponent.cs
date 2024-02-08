using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public interface IComponent
    {
        void Attach(IComponentHost host);

        void Detach();

        bool IsEnabled { get; set; }

        IComponentHost? Host { get; }    
    }
}
