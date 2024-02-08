using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public abstract class Behavior<T> : IBehavior where T : IComponentHost
    {
        protected float _startTime;
        private T? _host;

        public virtual void Start(RenderContext ctx)
        {

        }

        public virtual void Update(RenderContext ctx)
        {

        }

        void IComponent.Attach(IComponentHost host)
        {
           _host = (T)host; 
        }

        void IComponent.Detach()
        {
            _host = default;
        }

        public bool IsEnabled { get; set; }

        IComponentHost? IComponent.Host => _host;
    }
}
