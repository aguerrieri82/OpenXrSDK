using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine
{
    public interface IGeometryComponent : IComponent<Geometry3D>
    {
        void NotifyLoaded() { }

        void UpdateBounds() { }

    }
}
