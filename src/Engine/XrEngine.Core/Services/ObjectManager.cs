﻿using System.Diagnostics;

namespace XrEngine
{
    public class ObjectManager
    {
        readonly Dictionary<string, Type> _typeMap = [];

        public ObjectManager() { }

        public object CreateObject(string typeName)
        {
            Debug.WriteLine($"Create new {typeName}");

            var type = FindType(typeName);

            if (type == null)
                throw new NotSupportedException($"Type '{typeName}' not found");

            return Activator.CreateInstance(type)!;
        }

        public Type? FindType(string typeName)
        {
            if (!_typeMap.TryGetValue(typeName, out var type))
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(typeName);
                    if (type != null)
                    {
                        _typeMap[typeName] = type;
                        break;
                    }
                }
            }

            return type;
        }

        public static readonly ObjectManager Instance = new();
    }
}
