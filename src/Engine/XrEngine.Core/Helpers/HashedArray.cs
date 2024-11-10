using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public struct HashedArray<T>
    {
        readonly HashSet<T> _hash;

        public HashedArray()
        {
            _hash = [];
            Data = [];
        }

        public void Add(T value)
        {
            if (_hash.Add(value))
                Data = _hash.ToArray();
        }

        public T[] Data;
    }
}
