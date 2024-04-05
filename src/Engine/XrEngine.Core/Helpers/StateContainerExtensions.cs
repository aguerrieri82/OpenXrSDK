using System.Reflection;

namespace XrEngine
{


    public static class StateContainerExtensions
    {
        #region STATE



        public static void WriteArray<T>(this IStateContainer container, string key, IList<T> items) where T : class, IStateManager
        {
            var arrayState = container.Enter(key);
            for (var i = 0; i < items.Count; i++)
            {
                if (!items[i].GetType().HasEmptyConstructor())
                    continue;
                arrayState.Write(i.ToString(), items[i]);
            }
        }

        public static void ReadArray<T>(this IStateContainer container, string key, IList<T> curItems, Action<T> addItem, Action<T> removeItem) where T : class, IObjectId, IStateManager
        {
            HashSet<T> foundItems = [];

            if (container.Contains(key))
            {
                curItems ??= [];

                var arrayState = container.Enter(key);

                foreach (var childKey in arrayState.Keys)
                {
                    var itemState = arrayState.Enter(childKey, true);
                    var itemId = itemState.Read<uint>("Id");
                    var curItem = curItems!.FirstOrDefault(a => a.Id == itemId);

                    if (curItem == null)
                        curItem = arrayState.Read<T>(childKey);
                    else
                        curItem.SetState(itemState);

                    addItem(curItem!);

                    foundItems.Add(curItem!);
                }
            }

            if (curItems != null)
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


        public static void WriteObject<T>(this IStateContainer container, T obj)
        {
            foreach (var prop in typeof(T).GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.CanWrite && prop.CanRead)
                    container.Write(prop.Name, prop.GetValue(obj));
            }

        }

        public static void ReadObject<T>(this IStateContainer container, T obj)
        {
            ReadObject(container, obj!, typeof(T));
        }

        public static void ReadObject(this IStateContainer container, object obj, Type objType)
        {
            foreach (var prop in objType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.CanWrite && prop.CanRead && container.Contains(prop.Name))
                    prop.SetValue(obj, container.Read(prop.Name, prop.PropertyType));
            }
        }


        public static void WriteTypedObject(this IStateContainer container, string key, IStateManager value)
        {
            var objState = container.Enter(key);
            objState.WriteTypeName(value);
            value.GetState(objState);
        }

        public static T ReadTypedObject<T>(this IStateContainer container, string key) where T : IStateManager
        {
            var objState = container.Enter(key);
            var typeName = objState.ReadTypeName();
            if (typeName == null)
                throw new InvalidOperationException("Type name not found");

            var obj = (T)ObjectManager.Instance.CreateObject(typeName!);
            obj.SetState(objState);

            if (obj is IObjectId objId)
                container.Context.RefTable.Resolved[objId.Id] = obj;

            return obj;
        }

        public static T Read<T>(this IStateContainer container, string key)
        {
            return (T)container.Read(key, typeof(T))!;
        }

        #endregion

    }
}
