using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine
{
    public class Object3DInstance : Object3D
    {

        public override void UpdateWorldBounds()
        {
            if (Reference == null)
                return;

            _worldBounds = Reference.WorldBounds
                .Transform(Reference.WorldMatrixInverse)
                .Transform(WorldMatrix);

            base.UpdateWorldBounds();
        }

        public override T? Feature<T>() where T : class
        {
            var result = Reference?.Feature<T>();
            if (result != null)
                return result;
            return base.Feature<T>();
        }


        public Object3D? Reference { get; set; } 
    }
}
