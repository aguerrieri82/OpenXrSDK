using System.Reflection;

namespace XrEngine
{
    public static class StateContainerExtensions
    {
        public static void WriteArray<T>(this IStateContainer container, string key, IList<T> items) where T : class, IStateObject
        {
            IStateContainer arrayState = container.Enter(key);
            for (int i = 0; i < items.Count; i++)
            {
                if (!items[i].GetType().HasEmptyConstructor())
                    continue;
                arrayState.Write(i.ToString(), items[i]);
            }
        }

        public static void ReadArray<T>(this IStateContainer container, string key, IList<T> curItems, Action<T> addItem, Action<T> removeItem) where T : class, IStateObject
        {
            HashSet<T> foundItems = [];

            bool isUpdate = container.Context.Is(StateContextFlags.Update);

            if (container.Contains(key))
            {
                curItems ??= [];

                IStateContainer arrayState = container.Enter(key);

                foreach (string childKey in arrayState.Keys)
                {
                    IStateContainer itemState = arrayState.Enter(childKey, true);
                    Guid itemId = itemState.Read<Guid>("Id");
                    T? curItem = curItems!.FirstOrDefault(a => a.Id.Value == itemId);

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
                for (int i = curItems.Count - 1; i >= 0; i--)
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
                Span<byte> buffer = new Span<byte>((byte*)pBuffer, value.Length * sizeof(T));
                string base64 = Convert.ToBase64String(buffer);
                container.Write(key, base64);
            }
        }

        public static unsafe T[] ReadBuffer<T>(this IStateContainer container, string key) where T : unmanaged
        {
            string base64 = container.Read<string>(key);
            byte[] bytes = Convert.FromBase64String(base64);

            fixed (byte* pBuffer = bytes)
            {
                Span<T> buffer = new Span<T>((T*)pBuffer, bytes.Length / sizeof(T));
                return buffer.ToArray();
            }
        }

        public static void WriteObject<T>(this IStateContainer container, object obj)
        {
            container.WriteObject(obj, typeof(T));
        }

        public static void WriteObject(this IStateContainer container, object obj, Type objType)
        {
            StateManagerAttribute? sm = objType.GetCustomAttribute<StateManagerAttribute>();
            bool isExplicit = sm != null && sm.Mode == StateManagerMode.Explicit;
            foreach (PropertyInfo prop in objType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
            {
                SaveStateAttribute? saveState = prop.GetCustomAttribute<SaveStateAttribute>();
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
                IStateContainer itemState = container.Enter(key, true);
                if (itemState == null)
                    return null;
                Guid itemId = itemState.Read<Guid>("Id");
                if (curObj.Id.Value == itemId)
                    return container.Read(key, curObj);

            }
            return container.Read<T>(key);
        }


        public static void ReadObject(this IStateContainer container, object obj, Type objType)
        {
            StateManagerAttribute? sm = objType.GetCustomAttribute<StateManagerAttribute>();
            bool isExplicit = sm != null && sm.Mode == StateManagerMode.Explicit;

            foreach (PropertyInfo prop in objType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
            {
                SaveStateAttribute? saveState = prop.GetCustomAttribute<SaveStateAttribute>();

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
            IStateContainer objState = container.Enter(key);
            objState.WriteTypeName(value);
            value.GetState(objState);
        }

        public static T CreateTypedObject<T>(this IStateContainer container, string key) where T : IStateManager
        {
            IStateContainer objState = container.Enter(key);
            string? typeName = objState.ReadTypeName();
            if (typeName == null)
                throw new InvalidOperationException($"Type name '{typeName}' not found");

            T obj = (T)ObjectManager.Instance.CreateObject(typeName!);
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
