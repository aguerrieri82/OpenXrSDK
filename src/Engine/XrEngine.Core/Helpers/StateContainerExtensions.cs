using System.Reflection;

namespace XrEngine
{


    public static class StateContainerExtensions
    {

        public static void WriteArray<T>(this IStateContainer container, string key, IList<T> items) where T : class, IStateObject
        {
            var arrayState = container.Enter(key);
            for (var i = 0; i < items.Count; i++)
            {
                if (!items[i].GetType().HasEmptyConstructor())
                    continue;
                arrayState.Write(i.ToString(), items[i]);
            }
        }

        public static void ReadArray<T>(this IStateContainer container, string key, IList<T> curItems, Action<T> addItem, Action<T> removeItem) where T : class, IStateObject
        {
            HashSet<T> foundItems = [];

            var isUpdate = container.Context.Is(StateContextFlags.Update);

            if (container.Contains(key))
            {
                curItems ??= [];

                var arrayState = container.Enter(key);

                foreach (var childKey in arrayState.Keys)
                {
                    var itemState = arrayState.Enter(childKey, true);
                    var itemId = itemState.Read<Guid>("Id");
                    var curItem = curItems!.FirstOrDefault(a => a.Id.Value == itemId);

                    if (curItem == null)
                    {
                        if (!isUpdate)
                        {
                            curItem = arrayState.Read<T>(childKey);
                            addItem(curItem!);
                        }
                    }
                    else
                        arrayState.Read(childKey, curItem);

                    foundItems.Add(curItem!);
                }
            }

            if (curItems != null && !isUpdate)
            {
                for (var i = curItems.Count - 1; i >= 0; i--)
                {
                    if (!foundItems.Contains(curItems[i]))
                        removeItem(curItems[i]);
                }
            }
        }


        public static void WriteTypeName(this IStateContainer container, object? obj)
        {
            if (obj != null)
                container.Write("$type", obj.GetType().FullName);
        }

        public static string? ReadTypeName(this IStateContainer container)
        {
            if (container.Contains("$type"))
                return container.Read<string>("$type");
            return null;
        }

        public static unsafe void WriteBuffer<T>(this IStateContainer container, string key, T[] value) where T : unmanaged
        {
            fixed (T* pBuffer = value)
            {
                var buffer = new Span<byte>((byte*)pBuffer, value.Length * sizeof(T));
                var base64 = Convert.ToBase64String(buffer);
                container.Write(key, base64);
            }
        }

        public static unsafe T[] ReadBuffer<T>(this IStateContainer container, string key) where T : unmanaged
        {
            var base64 = container.Read<string>(key);
            var bytes = Convert.FromBase64String(base64);

            fixed (byte* pBuffer = bytes)
            {
                var buffer = new Span<T>((T*)pBuffer, bytes.Length / sizeof(T));
                return buffer.ToArray();
            }
        }

        public static void WriteObject<T>(this IStateContainer container, object obj)
        {
            container.WriteObject(obj, typeof(T));
        }

        public static void WriteObject(this IStateContainer container, object obj, Type objType)
        {
            var sm = objType.GetCustomAttribute<StateManagerAttribute>();
            var isExplicit = sm != null && sm.Mode == StateManagerMode.Explicit;
            foreach (var prop in objType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
            {
                var saveState = prop.GetCustomAttribute<SaveStateAttribute>();
                if (isExplicit && saveState == null)
                    continue;
                if (saveState != null && !saveState.IsSave)
                    continue;
                if (prop.CanWrite && prop.CanRead)
                    container.Write(prop.Name, prop.GetValue(obj));
            }
        }

        public static void ReadObject<T>(this IStateContainer container, T obj)
        {
            ReadObject(container, obj!, typeof(T));
        }

        public static T? ReadObject<T>(this IStateContainer container, string key, T? curObj) where T : class, IStateObject
        {
            if (curObj != null)
            {
                var itemState = container.Enter(key, true);
                if (itemState == null)
                    return null;
                var itemId = itemState.Read<Guid>("Id");
                if (curObj.Id.Value == itemId)
                    return container.Read(key, curObj);

            }
            return container.Read<T>(key);
        }


        public static void ReadObject(this IStateContainer container, object obj, Type objType)
        {
            var sm = objType.GetCustomAttribute<StateManagerAttribute>();
            var isExplicit = sm != null && sm.Mode == StateManagerMode.Explicit;

            foreach (var prop in objType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
            {
                var saveState = prop.GetCustomAttribute<SaveStateAttribute>();

                if (isExplicit && saveState == null)
                    continue;
                if (saveState != null && !saveState.IsSave)
                    continue;
                if (prop.CanWrite && prop.CanRead && container.Contains(prop.Name))
                    prop.SetValue(obj, container.Read(prop.Name, prop.GetValue(obj), prop.PropertyType));
            }
        }

        public static void WriteTypedObject(this IStateContainer container, string key, IStateManager value)
        {
            var objState = container.Enter(key);
            objState.WriteTypeName(value);
            value.GetState(objState);
        }

        public static T CreateTypedObject<T>(this IStateContainer container, string key) where T : IStateManager
        {
            var objState = container.Enter(key);
            var typeName = objState.ReadTypeName();
            if (typeName == null)
                throw new InvalidOperationException($"Type name '{typeName}' not found");

            var obj = (T)ObjectManager.Instance.CreateObject(typeName!);
            obj.SetState(objState);

            if (obj is IObjectId objId)
                container.Context.RefTable.Resolved[objId.Id] = obj;

            return obj;
        }

        public static object? Read(this IStateContainer container, string key, Type type)
        {
            return container.Read(key, null, type);
        }

        public static T Read<T>(this IStateContainer container, string key, T? curObj) where T : class
        {
            return (T)container.Read(key, curObj, typeof(T))!;
        }

        public static T Read<T>(this IStateContainer container, string key)
        {
            return (T)container.Read(key, typeof(T))!;
        }

        public static bool Is(this IStateContext self, StateContextFlags flag)
        {
            return (self.Flags & flag) == flag;
        }

        public static bool Is(this IStateContainer self, StateContextFlags flag)
        {
            return self.Context.Is(flag);
        }


    }
}
