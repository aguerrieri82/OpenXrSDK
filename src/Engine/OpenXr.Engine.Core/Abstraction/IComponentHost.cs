using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public interface IComponentHost
    {
        void AddComponent(IComponent component);

        void RemoveComponent(IComponent component);

        IEnumerable<T> Components<T>() where T : IComponent;
    }
}
