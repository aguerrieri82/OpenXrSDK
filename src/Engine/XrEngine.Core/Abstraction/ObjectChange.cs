namespace XrEngine
{
    [Flags]
    public enum ObjectChangeType
    {
        Unspecified = 0,
        Visibility = 1,
        Parent = 2,
        Transform = 4,
        Render = 8,
        SceneAdd = Parent | 0x10,
        Geometry = 0x20,
        Components = 0x40,
        SceneRemove = Parent | 0x80,
        Property = 0x100
    }

    public readonly struct ObjectChange
    {
        public ObjectChange(ObjectChangeType type, EngineObject? target = null, IList<string>? properties = null)
        {
            Type = type;
            Target = target;
            Properties = properties;
        }

        public bool IsAny(params ObjectChangeType[] types)
        {
            foreach (var t in types)
                if ((Type & t) == t)
                    return true;
            return false;
        }


        public static implicit operator ObjectChange(ObjectChangeType type)
        {
            return new ObjectChange(type);
        }


        public readonly ObjectChangeType Type;

        public readonly EngineObject? Target;

        public readonly IList<string>? Properties;
    }
}
