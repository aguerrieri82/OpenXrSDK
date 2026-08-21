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
            _oldValues[key!] = curValue;
            return isChanged;
        }
        public void Clear()
        {
            _oldValues.Clear();
        }
    }
}
