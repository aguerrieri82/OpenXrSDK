﻿using XrEditor.Services;
using XrEngine;

namespace XrEditor
{
    public static class EditorExtensions
    {
        public static INode GetNode(this object obj)
        {
            return Context.Require<NodeManager>().CreateNode(obj);
        }

        public static T SetParent<T>(this T nodes, INode? parent) where T : IEnumerable<INode>
        {
            foreach (var node in nodes.OfType<IEditableNode>())
                node.SetParent(parent);
            return nodes;
        }

        public static Func<Task> ToTask(this Action action)
        {
            return () =>
            {
                action();
                return Task.CompletedTask;
            };
        }

    }
}
