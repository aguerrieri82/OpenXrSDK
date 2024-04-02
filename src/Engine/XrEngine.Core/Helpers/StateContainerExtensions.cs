using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using XrMath;

namespace XrEngine
{


    public static class StateContainerExtensions
    {
        #region STATE

        public static T? ReadTypedRef<T>(this IStateContainer container, string key) where T : class, IObjectId, IStateManager
        {
            return container.ReadRef<T>(key, (id, state) =>
            {
                var typeName = state.ReadTypeName();
                var result = (T)ObjectFactory.Instance.CreateObject(typeName!);
                result.SetState(state);
                return result;
            });
        }

        public static T? ReadRef<T>(this IStateContainer container, string key, Func<ObjectId, IStateContainer, T> resolver) where T : class, IObjectId
        {
            var id = container.Read<ObjectId>(key);

            var refTable = container.Context.RefTable;

            if (!refTable.Resolved.TryGetValue(id, out var result))
            {
                result = resolver(id, refTable.Container!.Enter(id.ToString()));
                refTable.Resolved[id] = result;
            }

            return (T?)result;
        }

        public static void WriteRef<T>(this IStateContainer container, string key, T? value) where T : class, IObjectId
        {
            if (value != null)
            {
                var refTable = container.Context.RefTable;
                var idKey = value.Id.ToString();

                if (!refTable.Container!.Contains(idKey))
                    refTable.Container!.Write(idKey, value);

                container.Write(key, value.Id);
            }
            else
                container.Write(key, null);
        }


        public static void WriteRefArray<T>(this IStateContainer container, string key, IList<T> items) where T : class, IStateManager, IObjectId
        {
            var arrayState = container.Enter(key);
            for (var i = 0; i < items.Count; i++)
                arrayState.WriteRef(i.ToString(), items[i]);
        }

        public static void WriteArray<T>(this IStateContainer container, string key, IList<T> items) where T : class, IStateManager
        {
            var arrayState = container.Enter(key);
            for (var i = 0; i < items.Count; i++)
            {
                if (!items[i].GetType().HasEmptyConstructor())
                    continue;
                arrayState.WriteTypedObject(i.ToString(), items[i]);
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
                    var itemState = arrayState.Enter(childKey);
                    var itemId = itemState.Read<uint>("Id");
                    var curItem = curItems!.FirstOrDefault(a => a.Id == itemId);

                    if (curItem == null)
                        curItem = arrayState.ReadTypedObject<T>(childKey);
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

        public static void ReadRefArray<T>(this IStateContainer container, string key, IList<T> curItems, Action<T> addItem, Action<T> removeItem) where T : class, IObjectId, IStateManager
        {
            HashSet<T> foundItems = [];

            if (container.Contains(key))
            {
                curItems ??= [];

                var arrayState = container.Enter(key);

                foreach (var childKey in arrayState.Keys)
                {
                    var itemId = arrayState.Read<ObjectId>(childKey);
                    
                    var curItem = curItems!.FirstOrDefault(a => a.Id == itemId);

                    if (curItem == null)
                        curItem = arrayState.ReadTypedRef<T>(childKey);
                    else
                        curItem.SetState(arrayState.Enter(childKey, true));

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
            foreach (var prop in typeof(T).GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
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
            var obj = (T)ObjectFactory.Instance.CreateObject(typeName!);
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
