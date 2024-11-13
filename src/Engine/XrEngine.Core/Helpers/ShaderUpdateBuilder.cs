using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using XrMath;


namespace XrEngine
{
    public delegate void UpdateUniformAction(UpdateShaderContext ctx, IUniformProvider up);

    public delegate void UpdateBufferAction(UpdateShaderContext ctx);

    public class ShaderUpdate
    {
        public List<UpdateUniformAction>? Actions;

        public List<UpdateBufferAction>? BufferUpdates;

        public List<string>? Features;

        public List<string>? Extensions;

        public long ShaderVersion;

        public string? LightsHash;

        public string? FeaturesHash;

        public IShaderHandler?[]? ShaderHandlers;
    }

    public class UpdateShaderContext
    {
        public UpdateShaderContext()
        {
            FrustumPlanes = new Plane[6];
        }

        public Camera? Camera;

        public Shader? Shader;

        public IList<Light>? Lights;

        public Object3D? Model;

        public IRenderEngine? RenderEngine;

        public VertexComponent ActiveComponents;

        public IBufferProvider? BufferProvider;

        public IBuffer? CurrentBuffer;

        public string? LightsHash;

        public readonly Plane[] FrustumPlanes;

        public IShadowMapProvider? ShadowMapProvider;

        public IBloomProvider? BloomProvider;

        public Texture2D? DepthMap;

        public ShaderUpdate? LastGlobalUpdate;

        public IRenderPass? Pass;

        public long ContextVersion;

        public long Frame;
    }

    public readonly struct ShaderUpdateBuilder : IFeatureList
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

        public readonly void LoadBuffer<T>(UpdateAction<T?> value, int slot, BufferStore store) where T : struct
        {
            _result.BufferUpdates!.Add((ctx) =>
            {
                var buffer = ctx.BufferProvider!.GetBuffer<T>(slot, store);

                ctx.CurrentBuffer = buffer;

                var curValue = value(ctx);

                if (curValue != null)
                    buffer.Update(curValue.Value);

                ctx.CurrentBuffer = null;
            });

            _result.Actions!.Add((ctx, up) =>
            {
                var buffer = ctx.BufferProvider!.GetBuffer<T>(slot, store);
                up.LoadBuffer(buffer, slot);
            });
        }


        public readonly void ExecuteAction(UpdateUniformAction action)
        {
            _result.Actions!.Add(action);
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
