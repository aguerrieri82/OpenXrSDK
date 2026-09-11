using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine
{
    public static class DictionaryExtensions
    {
        public static TValue GetOrAdd<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            Func<TKey, TValue> valueFactory)
        {
            lock (dictionary)
            {
                if (dictionary.TryGetValue(key, out var value))
                    return value;

                value = valueFactory(key);
                dictionary.Add(key, value);

                return value;
            }
        }

        public static bool TryRemove<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            out TValue value)
        {
            lock (dictionary)
            {
                if (!dictionary.TryGetValue(key, out value!))
                    return false;

                return dictionary.Remove(key);
            }
        }
    }
}
