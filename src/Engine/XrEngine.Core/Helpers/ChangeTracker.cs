using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine
{
    public class ChangeTracker
    {
        readonly Dictionary<Func<object>, object> _oldValues = [];

        public bool IsChanged(Func<object> getter)
        {
            var curValue = getter();
            var isChanged = !_oldValues.TryGetValue(getter, out var oldValue) || !Equals(oldValue, curValue);
            _oldValues[getter] = curValue;
            return isChanged;
        }
        public void Clear()
        {
            _oldValues.Clear();
        }
    }
}
