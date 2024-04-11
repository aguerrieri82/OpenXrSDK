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
        Property = 0x100,
        ChildAdd = 0x200,
        ChildRemove = 0x400
    }

    public struct ObjectChangeSet
    {
        public ObjectChangeSet()
        {
        }

        public void Add(ObjectChange change)
        {
            Changes ??= [];

            var curChangeIndex = Changes.FindIndex(a => a.Target == change.Target);

            if (curChangeIndex != -1)
            {
                var curChange = Changes[curChangeIndex];
                curChange.Type |= change.Type;

                if (change.Properties != null)
                {
                    curChange.Properties ??= new List<string>();
                    foreach (var prop in change.Properties)
                    {
                        if (!curChange.Properties.Contains(prop))
                            curChange.Properties.Add(prop);
                    }
                }

                Changes[curChangeIndex] = curChange;
            }
            else
                Changes.Add(change);
        }

        public void Clear()
        {
            Changes?.Clear();
        }

        public List<ObjectChange> Changes;
    }

    public struct ObjectChange
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


        public ObjectChangeType Type;

        public EngineObject? Target;

        public IList<string>? Properties;
    }
}
