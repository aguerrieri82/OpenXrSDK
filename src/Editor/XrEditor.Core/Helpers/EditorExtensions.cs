using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEditor.Services;

namespace XrEditor.Helpers
{
    public static class EditorExtensions
    {
        public static INode GetNode(this object obj)
        {
            return Context.Require<NodeManager>().CreateNode(obj);
        }
    }
}
