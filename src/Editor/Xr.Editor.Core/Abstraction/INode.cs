using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Editor
{
    public interface INode
    {
        IEnumerable<INode> Children { get; }

        IEnumerable<INode> Components { get; }

        public ICollection<string> Types { get; }

        public object Value { get; }

        public INode? Parent { get; }
    }
}
