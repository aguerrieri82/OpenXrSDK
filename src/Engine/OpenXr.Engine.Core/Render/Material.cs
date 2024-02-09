using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public abstract class Material : EngineObject
    {
        protected Object3D? _host;

        public void Attach(Object3D host)
        {
            _host = host;
        }

        public void Detach()
        {
            _host = null;
        }

        public virtual void NotifyChanged()
        {
            _host?.NotifyChanged();
        }

        public Color Color {  get; set; }   
    }
}
