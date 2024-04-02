using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrEngine.Services;
using XrMath;

namespace XrEngine
{
    public class TypeStateManager
    {
        #region DefaultManager

        public class DefaultManager : ITypeStateManager<IStateManager>
        {
            public IStateManager Read(string key, Type objType, IStateContainer container)
            {
                var obj = (IStateManager)Activator.CreateInstance(objType)!;
                obj.SetState(container.Enter(key));
                return obj;
            }

            public void Write(string key, IStateManager obj, IStateContainer container)
            {
                obj.GetState(container.Enter(key));
            }
        }

        #endregion DefaultManager

        #region Vector3Manager

        public class Vector3Manager : ITypeStateManager<Vector3>
        {
            public Vector3 Read(string key, Type objType, IStateContainer container)
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
            public Color Read(string key, Type objType, IStateContainer container)
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
            public Matrix4x4 Read(string key, Type objType, IStateContainer container)
            {
                var array = container.Read<float[]>(key);
                fixed (float* pArray = array)
                    return *(Matrix4x4*)pArray;
            }

            public  void Write(string key, Matrix4x4 obj, IStateContainer container)
            {
                var floats = new Span<float>(&obj, 16);
                container.Write(key, floats.ToArray());
            }
        }

        #endregion

        #region QuaternionManager

        public class QuaternionManager : ITypeStateManager<Quaternion>
        {
            public Quaternion Read(string key, Type objType, IStateContainer container)
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
            public EngineObject? Read(string key, Type objType, IStateContainer container)
            {
                var objState = container.Enter(key);
                
                if (objState == null)
                    return null;

                if (objState.Contains("$uri"))
                {
                    var assetUri = objState.Read<Uri>("$uri");

                    return AssetLoader.Instance.Load(assetUri, objType, null);
                }
                else
                {
                    var typeName = objState.ReadTypeName();
                    var obj = (EngineObject)ObjectFactory.Instance.CreateObject(typeName!);
                    obj.SetState(objState);
                    return obj;
                }
            }

            public void Write(string key, EngineObject? obj, IStateContainer container)
            {
                var assetSrc = obj!.Components<AssetSource>().FirstOrDefault();
                var objState = container.Enter(key);

                if (assetSrc != null)
                    objState.Write("$uri", assetSrc.AssetUri);
                else
                {
                    objState.WriteTypeName(obj);
                    obj.GetState(objState);
                }
            }
        }

        #endregion

        #region EngineObjectManager

        public class ObjectIdManager : ITypeStateManager<ObjectId>
        {
            public ObjectId Read(string key, Type objType, IStateContainer container)
            {
                return new ObjectId() { Value = container.Read<uint>(key) };
            }

            public void Write(string key, ObjectId obj, IStateContainer container)
            {
                container.Write(key, obj.Value);
            }
        }

        #endregion

        List<ITypeStateManager> _types = [];
        
        public TypeStateManager()
        {
            Register(new Vector3Manager());
            Register(new ColorManager());
            Register(new Matrix4x4Manager());
            Register(new QuaternionManager());
            Register(new EngineObjectManager());
            Register(new DefaultManager());
            Register(new ObjectIdManager());
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
