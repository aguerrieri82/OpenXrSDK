using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public interface IObjectChangeListener
    {
        void NotifyChanged(Object3D object3D, ObjectChange change);
    }
}
