using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class TypeLayer<T> : BaseAutoLayer<T> where T : Object3D
    {

        protected override bool BelongsToLayer(T obj)
        {
            return true;
        }
    }
}
