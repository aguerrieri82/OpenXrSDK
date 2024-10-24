using System.Collections;

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
        Geometry = 0x20,
        Property = 0x40,
        Scene = 0x80,
        Components = 0x100,
        Children = 0x200,
        Add = 0x400,
        Remove = 0x800,
        Enabled = 0x1000,
        Material = 0x2000 | Render,
        SceneAdd = Parent | Add | Scene,
        SceneRemove = Parent | Remove | Scene,
        ChildAdd = Add | Children,
        ChildRemove = Remove | Children,
        ComponentAdd = Add | Components,
        ComponentRemove = Remove | Components,  
        ComponentEnabled = Enabled | Components,
        MateriaAdd = Add | Material,
        MateriaRemove = Remove | Material,
        MaterialEnabled = Enabled | Material,   

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
                    curChange.Properties ??= [];
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

        public List<ObjectChange>? Changes;
    }

    public struct ObjectChange
    {
        public ObjectChange(ObjectChangeType type, object? target = null, IList<string>? properties = null)
        {
            Type = type;
            Target = target;
            Properties = properties;
        }

        public readonly bool IsAny(params ObjectChangeType[] types)
        {
            foreach (var t in types)
                if ((Type & t) == t)
                    return true;
            return false;
        }

        public readonly IEnumerable<T> Targets<T>() where T : class
        {
            if (Target is T target)
                yield return target;

            else if (Target is IEnumerable targetEnum)
            {
                foreach (var targetChild in targetEnum.OfType<T>())
                    yield return targetChild;
            }
        }   

        public static implicit operator ObjectChange(ObjectChangeType type)
        {
            return new ObjectChange(type);
        }


        public ObjectChangeType Type;

        public object? Target;

        public IList<string>? Properties;
    }
}
