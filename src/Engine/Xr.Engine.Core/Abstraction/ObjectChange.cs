namespace Xr.Engine
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
        SceneRemove = Parent | 0x80
    }

    public readonly struct ObjectChange
    {
        public ObjectChange(ObjectChangeType type, EngineObject? target = null)
        {
            Type = type;
            Target = target;
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
    }
}
