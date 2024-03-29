using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine
{
    public class TypeStateManager
    {
        #region DefaultManager

        public class DefaultManager : ITypeStateManager<IStateManager>
        {
            public IStateManager Read(string key, Type objType, IStateContainer container, StateContext ctx)
            {
                var obj = (IStateManager)Activator.CreateInstance(objType)!;
                obj.SetState(ctx, container.Enter(key));
                return obj;
            }

            public void Write(string key, IStateManager obj, IStateContainer container, StateContext ctx)
            {
                obj.GetState(ctx, container.Enter(key));
            }
        }

        #endregion DefaultManager

        #region Vector3Manager

        public class Vector3Manager : ITypeStateManager<Vector3>
        {
            public Vector3 Read(string key, Type objType, IStateContainer container, StateContext ctx)
            {
                var parts = container.Read<float[]>(key);
                return new Vector3(parts[0], parts[1], parts[2]);   
            }

            public void Write(string key, Vector3 obj, IStateContainer container, StateContext ctx)
            {
                container.Write(key, new float[] { obj.X, obj.Y, obj.Z });
            }
        }

        #endregion

        #region ColorManager

        public class ColorManager : ITypeStateManager<Color>
        {
            public Color Read(string key, Type objType, IStateContainer container, StateContext ctx)
            {
                return Color.Parse(container.Read<string>(key));
            }

            public void Write(string key, Color obj, IStateContainer container, StateContext ctx)
            {
                container.Write(key, obj.ToHex());
            }
        }

        #endregion

        #region ColorManager

        public unsafe class Matrix4x4Manager : ITypeStateManager<Matrix4x4>
        {
            public Matrix4x4 Read(string key, Type objType, IStateContainer container, StateContext ctx)
            {
                var array = container.Read<float[]>(key);
                fixed (float* pArray = array)
                    return *(Matrix4x4*)pArray;
            }

            public  void Write(string key, Matrix4x4 obj, IStateContainer container, StateContext ctx)
            {
                var floats = new Span<float>(&obj, 16);
                container.Write(key, floats.ToArray());
            }
        }

        #endregion


        #region QuaternionManager

        public class QuaternionManager : ITypeStateManager<Quaternion>
        {
            public Quaternion Read(string key, Type objType, IStateContainer container, StateContext ctx)
            {
                var parts = container.Read<float[]>(key);
                return new Quaternion(parts[0], parts[1], parts[2], parts[3]);
            }

            public void Write(string key, Quaternion obj, IStateContainer container, StateContext ctx)
            {
                container.Write(key, new float[] { obj.X, obj.Y, obj.Z, obj.W });
            }
        }

        #endregion

        #region EngineObjectManager

        public class EngineObjectManager : ITypeStateManager<EngineObject>
        {
            public EngineObject Read(string key, Type objType, IStateContainer container, StateContext ctx)
            {
                var objState = container.Enter(key);
                if (objState.Contains("$uri"))
                {
                    var assetUri = objState.Read<string>("$uri");
                    throw new NotImplementedException();
                }
                else
                {
                    var typeName = objState.ReadTypeName();
                    var obj = (EngineObject)ObjectFactory.Instance.CreateObject(typeName!);
                    obj.SetState(ctx, objState);
                    return obj;
                }
            }

            public void Write(string key, EngineObject obj, IStateContainer container, StateContext ctx)
            {
                var assetSrc = obj.Components<AssetSource>().FirstOrDefault();
                var objState = container.Enter(key);

                if (assetSrc != null)
                    objState.Write("$uri", assetSrc.AssetUri);
                else
                {
                    objState.WriteTypeName(obj);
                    obj.GetState(ctx, objState);
                }
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
