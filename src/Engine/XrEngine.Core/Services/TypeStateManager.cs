using System.Numerics;
using XrEngine.Services;
using XrMath;

namespace XrEngine
{
    public class TypeStateManager
    {

        #region DefaultManager

        public class StateManager : ITypeStateManager<IStateManager>
        {
            StateManager() { }

            public IStateManager Read(string key, IStateManager? destObj, Type objType, IStateContainer container)
            {
                var obj = destObj ?? (IStateManager)Activator.CreateInstance(objType)!;
                obj.SetState(container.Enter(key));
                return obj;
            }

            public void Write(string key, IStateManager obj, IStateContainer container)
            {
                obj.GetState(container.Enter(key));
            }

            public static readonly StateManager Instance = new();
        }

        #endregion DefaultManager

        #region Vector3Manager

        public class Vector3Manager : ITypeStateManager<Vector3>
        {
            public Vector3 Read(string key, Vector3 destObj, Type objType, IStateContainer container)
            {
                var parts = container.Read<float[]>(key);
                return new Vector3(parts[0], parts[1], parts[2]);
            }

            public void Write(string key, Vector3 obj, IStateContainer container)
            {
                container.Write(key, new float[] { obj.X, obj.Y, obj.Z });
            }
        }

        #endregion

        #region ColorManager

        public class ColorManager : ITypeStateManager<Color>
        {
            public Color Read(string key, Color curObj, Type objType, IStateContainer container)
            {
                return Color.Parse(container.Read<string>(key));
            }

            public void Write(string key, Color obj, IStateContainer container)
            {
                container.Write(key, obj.ToHex());
            }
        }

        #endregion

        #region ColorManager

        public unsafe class Matrix4x4Manager : ITypeStateManager<Matrix4x4>
        {
            public Matrix4x4 Read(string key, Matrix4x4 curObj, Type objType, IStateContainer container)
            {
                var array = container.Read<float[]>(key);
                fixed (float* pArray = array)
                    return *(Matrix4x4*)pArray;
            }

            public void Write(string key, Matrix4x4 obj, IStateContainer container)
            {
                var floats = new Span<float>(&obj, 16);
                container.Write(key, floats.ToArray());
            }
        }

        #endregion

        #region QuaternionManager

        public class QuaternionManager : ITypeStateManager<Quaternion>
        {
            public Quaternion Read(string key, Quaternion curObj, Type objType, IStateContainer container)
            {
                var parts = container.Read<float[]>(key);
                return new Quaternion(parts[0], parts[1], parts[2], parts[3]);
            }

            public void Write(string key, Quaternion obj, IStateContainer container)
            {
                container.Write(key, new float[] { obj.X, obj.Y, obj.Z, obj.W });
            }
        }

        #endregion

        #region EngineObjectManager

        public class EngineObjectManager : ITypeStateManager<EngineObject?>
        {
            EngineObjectManager() { }

            public EngineObject? Read(string key, EngineObject? destObj, Type objType, IStateContainer container)
            {
                var objState = container.Enter(key, true);

                if (objState == null)
                    return null;

                if (objState.Contains("$uri"))
                {
                    var assetUri = objState.Read<Uri>("$uri");

                    return AssetLoader.Instance.Load(assetUri, objType, destObj);
                }

                return (EngineObject)StateObjectManager.Instance.Read(key, destObj, objType, container);
            }

            public void Write(string key, EngineObject? obj, IStateContainer container)
            {
                var assetSrc = obj!.Components<AssetSource>().FirstOrDefault();
                var objState = container.Enter(key);

                if (assetSrc != null)
                {
                    objState.Write("$uri", assetSrc.AssetUri);
                    objState.Write("Id", obj.Id);
                }
                else
                    StateObjectManager.Instance.Write(key, obj, container);
            }

            public static readonly EngineObjectManager Instance = new();
        }

        #endregion

        #region ObjectIdManager

        public class ObjectIdManager : ITypeStateManager<ObjectId>
        {
            public ObjectId Read(string key, ObjectId destObj, Type objType, IStateContainer container)
            {
                return new ObjectId() { Value = container.Read<Guid>(key) };
            }

            public void Write(string key, ObjectId obj, IStateContainer container)
            {
                container.Write(key, obj.Value);
            }
        }

        #endregion

        #region ObjectManager

        public class ObjectManager : ITypeStateManager<object?>
        {
            ObjectManager() { }

            public bool CanHandle(Type type)
            {
                return type.IsClass && type.HasEmptyConstructor();
            }

            public object? Read(string key, object? destObj, Type objType, IStateContainer container)
            {
                var keyContainer = container.Enter(key);
                if (keyContainer == null)
                    return null;
                var obj = destObj ?? Activator.CreateInstance(objType)!;
                keyContainer?.ReadObject(obj, objType);
                return obj;
            }

            public void Write(string key, object? obj, IStateContainer container)
            {
                if (obj != null)
                    container.Enter(key).WriteObject(obj, obj.GetType());
            }

            public static readonly ObjectManager Instance = new();
        }

        #endregion

        public class StateObjectManager : ITypeStateManager<IStateObject>
        {
            StateObjectManager() { }

            public IStateObject Read(string key, IStateObject? destObj, Type objType, IStateContainer container)
            {
                var id = container.Read<ObjectId>(key);

                var refTable = container.Context.RefTable;

                if (!refTable.Resolved.TryGetValue(id, out var result))
                {
                    result = refTable.Container!.ReadTypedObject<IStateObject>(id.ToString());
                    refTable.Resolved[id] = result;
                }

                return (IStateObject)result;
            }

            public void Write(string key, IStateObject value, IStateContainer container)
            {
                if (value != null)
                {
                    value.EnsureId();

                    var refTable = container.Context.RefTable;
                    var idKey = value.Id.ToString();

                    if (!refTable.Container!.Contains(idKey))
                        refTable.Container!.WriteTypedObject(idKey, value);

                    container.Write(key, value.Id);
                }
                else
                    container.Write(key, null);
            }

            public static readonly StateObjectManager Instance = new();
        }


        readonly List<ITypeStateManager> _types = [];

        public TypeStateManager()
        {
            Register(new Vector3Manager());
            Register(new ColorManager());
            Register(new Matrix4x4Manager());
            Register(new QuaternionManager());
            Register(new ObjectIdManager());

            Register(EngineObjectManager.Instance);
            Register(StateObjectManager.Instance);
            Register(StateManager.Instance);
            Register(ObjectManager.Instance);
        }

        public ITypeStateManager? Get(Type type)
        {
            return _types.FirstOrDefault(a => a.CanHandle(type));
        }

        public void Register(ITypeStateManager value)
        {
            _types.Add(value);
        }


        public static readonly TypeStateManager Instance = new();
    }
}
