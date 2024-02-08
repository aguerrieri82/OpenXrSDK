using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class Group : Object3D
    {
        List<Object3D> _children;

        public Group()
        {
            _children = new List<Object3D>();
        }

        public void AddChild(Object3D child)
        {

        }

        public IReadOnlyList<Object3D> Children => _children.AsReadOnly();
    }
}
