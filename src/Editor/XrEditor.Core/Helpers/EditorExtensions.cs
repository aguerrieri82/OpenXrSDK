using XrEditor.Services;
using XrEngine;

namespace XrEditor
{
    public static class EditorExtensions
    {
        public static INode GetNode(this object obj)
        {
            return Context.Require<NodeManager>().CreateNode(obj);
        }
    }
}
