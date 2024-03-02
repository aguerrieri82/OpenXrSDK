using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;


namespace Xr.Engine
{
    public delegate void UpdateUniformAction(UpdateShaderContext ctx, IUniformProvider up);


    public class ShaderUpdate
    {
        public List<UpdateUniformAction>? Actions;

        public List<UpdateUniformAction>? BufferUpdates;

        public List<string>? Features;

        public List<string>? Extensions;

        public long MaterialVersion;

        public long LightsVersion;

        public string? FeaturesHash;

    }

    public class UpdateShaderContext
    {
        public Camera? Camera;

        public IEnumerable<Light>? Lights;

        public Object3D? Model;

        public IRenderEngine? RenderEngine;

        public VertexComponent ActiveComponents;

        public long LightsVersion;
    }

    public struct ShaderUpdateBuilder : IFeatureList
    {
        private readonly ShaderUpdate _result;

        public delegate TValue UpdateAction<TValue>(UpdateShaderContext ctx);

        public ShaderUpdateBuilder(UpdateShaderContext context)
        {
            _result = new ShaderUpdate()
            {
                Features = [],
                Actions = [],
                BufferUpdates = [],
                Extensions = []
            };

            Context = context;
        }

        readonly void Update<TValue>(UpdateAction<TValue> action, Action<IUniformProvider, TValue> doUpdate)
        {
            _result.Actions!.Add((ctx, up) => doUpdate(up, action(ctx)));
        }

        public readonly void SetUniform(string name, UpdateAction<int> value, bool optional = false)
        {
            Log(name, value);
            Update(value, (up, v) => up.SetUniform(name, v, optional));
        }

        public readonly void SetUniform(string name, UpdateAction<Matrix4x4> value, bool optional = false)
        {
            Log(name, value);
            Update(value, (up, v) => up.SetUniform(name, v, optional));
        }

        public readonly void SetUniformBuffer<T>(string name, UpdateAction<T> value, bool optional = false)
        {
            Log(name, value);
            _result.BufferUpdates!.Add((ctx, up) => up.SetUniformBuffer(name, value(ctx), optional, true));
            _result.Actions!.Add((ctx, up) => up.SetUniformBuffer(name, default(T), optional, false));
        }

        public readonly void SetUniform(string name, UpdateAction<float> value, bool optional = false)
        {
            Log(name, value);
            Update(value, (up, v) => up.SetUniform(name, v, optional));
        }

        public readonly void SetUniform(string name, UpdateAction<Vector2I> value, bool optional = false)
        {
            Log(name, value);
            Update(value, (up, v) => up.SetUniform(name, v, optional));
        }

        public readonly void SetUniform(string name, UpdateAction<Vector3> value, bool optional = false)
        {
            Log(name, value);
            Update(value, (up, v) => up.SetUniform(name, v, optional));
        }

        public readonly void SetUniform(string name, UpdateAction<Color> value, bool optional = false)
        {
            Log(name, value);
            Update(value, (up, v) => up.SetUniform(name, v, optional));
        }

        public readonly void LoadTexture(UpdateAction<Texture2D> value, int slot = 0)
        {
            Update(value, (up, v) => up.LoadTexture(v, slot));
        }

        public readonly void SetUniform(string name, UpdateAction<Texture2D> value, int slot = 0, bool optional = false)
        {
            Log(name, slot);
            Update(value, (up, v) => up.SetUniform(name, v, slot, optional));
        }

        public readonly void SetUniform(string name, UpdateAction<float[]> value, bool optional = false)
        {
            Log(name, value);
            Update(value, (up, v) => up.SetUniform(name, v, optional));
        }

        public readonly void SetUniform(string name, UpdateAction<int[]> value, bool optional = false)
        {
            Log(name, value);
            Update(value, (up, v) => up.SetUniform(name, v, optional));
        }

        public readonly void SetUniform(string name, UpdateAction<IBuffer> value, bool optional = false)
        {
            Log(name, value);
            Update(value, (up, v) => up.SetUniform(name, v, optional));
        }


        public readonly void SetUniformConstStruct(string name, object obj, bool optional = false)
        {
            foreach (var field in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                var fullName = $"{name}.{field.Name}";
                SetUniformConst(fullName, () => field.GetValue(obj)!, field.FieldType, optional);
            }
        }

        public readonly void SetUniformConstStructArray(string name, ICollection collection, bool optional = false)
        {
            var i = 0;
            foreach (var item in collection)
            {
                SetUniformConstStruct($"{name}[{i}]", item, optional);
                i++;
            }
        }

        public readonly void SetUniformConst(string name, Func<object> getValue, Type objType, bool optional = false)
        {
            if (objType == typeof(Vector3))
                SetUniform(name, ctx => (Vector3)getValue(), optional);

            else if (objType == typeof(Color))
                SetUniform(name, ctx => (Color)getValue(), optional);

            else if (objType == typeof(Matrix4x4))
                SetUniform(name, ctx => (Matrix4x4)getValue(), optional);

            else if (objType == typeof(float))
                SetUniform(name, ctx => (float)getValue(), optional);

            else if (objType == typeof(int))
                SetUniform(name, ctx => (int)getValue(), optional);

            else if (objType == typeof(float[]))
                SetUniform(name, ctx => (float[])getValue(), optional);

            else if (objType == typeof(int[]))
                SetUniform(name, ctx => (int[])getValue(), optional);

            else
            {
                if (objType.IsValueType && !objType.IsEnum && !objType.IsPrimitive)
                    SetUniformConstStruct(name, getValue(), optional);

                else if (typeof(ICollection).IsAssignableFrom(objType))
                {
                    var gen = objType.GetInterfaces()
                            .First(a => a.IsGenericType && a.GetGenericTypeDefinition() == typeof(ICollection<>));

                    var elType = gen.GetGenericArguments()[0];

                    if (elType.IsValueType && !elType.IsEnum && !elType.IsPrimitive)
                        SetUniformConstStructArray(name, (ICollection)getValue(), optional);
                }
                else
                    throw new NotSupportedException();
            }
        }




        public readonly void AddFeature(string name)
        {
            _result.Features!.Add(name);
        }

        public readonly void AddExtension(string name)
        {
            _result.Extensions!.Add(name);
        }

        public readonly void ComputeHash(string shaderId)
        {
            _result.FeaturesHash = _result.Features!.Count == 0 ?
                shaderId :
                string.Concat(shaderId, ":", Convert.ToBase64String(MD5.HashData(Encoding.UTF8.GetBytes(string.Join(',', _result.Features)))));
        }

        readonly void Log(string name, object value)
        {
            //Logs.Append(name).Append(" = ").Append(value).AppendLine();
        }

        public StringBuilder Logs { get; } = new StringBuilder();

        public UpdateShaderContext Context { get; }

        public readonly ShaderUpdate Result => _result;
    }
}
