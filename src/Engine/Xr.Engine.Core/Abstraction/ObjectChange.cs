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
        Components = 0x40
    }

    public readonly struct ObjectChange
    {
        ObjectChange(ObjectChangeType type)
        {
            Type = type;
        }

        public bool IsAny(params ObjectChangeType[] types)
        {
            foreach (var t in types)
                if ((Type & t) == t)
                    return true;
            return false;
        }

        public readonly ObjectChangeType Type;

        public static implicit operator ObjectChange(ObjectChangeType type)
        {
            return new ObjectChange(type);
        }
    }
}
