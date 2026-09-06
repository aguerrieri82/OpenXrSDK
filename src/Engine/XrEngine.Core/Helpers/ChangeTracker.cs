using System.Runtime.CompilerServices;

namespace XrEngine
{
    public class ChangeTracker
    {
        readonly Dictionary<string, object?> _oldValues = [];

        public bool IsChanged(Func<object?> getter, [CallerArgumentExpression(nameof(getter))] string? key = null)
        {
            var curValue = getter();
            var isChanged = !_oldValues.TryGetValue(key!, out var oldValue) || !Equals(oldValue, curValue);
            /*
#if DEBUG
            if (isChanged)
                Log.Debug(this, "{0} changed", key);
#endif
            */
            _oldValues[key!] = curValue;
            return isChanged;
        }
        public void Clear()
        {
            _oldValues.Clear();
        }
    }
}
