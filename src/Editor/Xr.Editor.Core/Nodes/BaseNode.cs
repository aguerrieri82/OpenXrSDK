﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Editor.Nodes
{
    public abstract class BaseNode<T> : INode where T: notnull
    {
        protected INode? _parent;
        protected T _value;

        public BaseNode(T value)
        {
            _value = value;
        }

        public virtual bool IsLeaf => false;

        public virtual IEnumerable<INode> Children => [];

        public IEnumerable<INode> Components => throw new NotImplementedException();

        public ICollection<string> Types => throw new NotImplementedException();

        public T Value => _value;

        object INode.Value => _value;

        public INode? Parent => _parent;
    }
}