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
        protected T? _host;

        public Behavior()
        {
            IsEnabled = true;
        }

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


    public class LambdaBehavior<T> : Behavior<T> where T : IComponentHost
    {
        readonly Action<T, RenderContext> _update;

        public LambdaBehavior(Action<T, RenderContext> update)
        {
            _update = update;
        }

        public override void Update(RenderContext ctx)
        {
            _update(_host!, ctx);
        }
    }
}
