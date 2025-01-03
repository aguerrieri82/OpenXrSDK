﻿using System.Reflection;

namespace CanvasUI
{
    [Flags]
    public enum UiPropertyFlags
    {
        None,
        Layout = 0x1,
        Render = 0x2
    }


    public class UiProperty
    {
        public UiProperty(string name, Type type, Type ownerType)
        {
            Name = name;
            OwnerType = ownerType;
            Type = type;
        }

        public string Name;

        public Type Type;

        public object? DefaultValue;

        public Type OwnerType;

        public UiPropertyFlags Flags;

        public MethodInfo? OnChangedMethod;
    }


    public class UiProperty<T> : UiProperty
    {
        public UiProperty(string name, Type ownerType)
            : base(name, typeof(T), ownerType)
        {
        }
    }
}
