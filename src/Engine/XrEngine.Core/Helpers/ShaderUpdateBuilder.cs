using System.Collections;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Text;
using XrEngine.Helpers;
using XrMath;

namespace XrEngine
{
    public delegate void UpdateUniformAction(UpdateShaderContext ctx, IUniformProvider up);

    public delegate void UpdateBufferAction(UpdateShaderContext ctx);

    public class ShaderUpdate
    {
        public readonly List<UpdateUniformAction> Actions = [];

        public List<UpdateBufferAction>? BufferUpdates;

        public SortedSet<string>? Features;

        public SortedSet<string>? DynamicFeatures;

        public HashSet<string>? Extensions;

        public long ShaderVersion;

        public ulong LightsHash;

        public ulong FeaturesHash;

        public IShaderHandler?[]? ShaderHandlers;
    }

    public enum UpdateShaderStage
    {
        Any,
        Shader,
        Material,
        Model
    }

    public struct RenderDriverBugs
    {
        /// <summary>
        /// Workaround for an NVIDIA Vulkan multiview clipping bug observed with per-view <c>gl_ClipDistance</c>.
        /// If a primitive is completely rejected by clip distance in view 0, the corresponding primitive may also be
        /// incorrectly discarded from view 1 even when view 1's clip distances would keep it visible.
        /// 
        /// When this workaround is enabled, view 0 does not use the per-view clip distances and its excluded region is
        /// masked by a depth-prefill pass instead. View 1 continues to use normal clip-distance rejection.
        /// </summary>
        public bool NvMultiViewClipBug;
    }

    public class UpdateShaderContext
    {
        public UpdateShaderContext()
        {
            FrustumPlanes = new Plane[6];
        }

        public RenderDriverBugs Bugs;

        public UpdateShaderStage Stage;

        public Camera? MainCamera;

        public Camera? PassCamera;

        public Scene3D? Scene;

        public Object3D? Model;

        public Shader? Shader;

        public IList<Light>? Lights;

        public long ImageLightVersion;

        public long Frame;

        public float Time;

        public float DeltaTime;

        public IRenderEngine? RenderEngine;

        public IRenderPass? Pass;

        public VertexComponent ActiveComponents;

        public IBufferProvider? BufferProvider;

        public ISimpleBuffer? CurrentBuffer;

        public ulong LightsHash;

        public Plane[] FrustumPlanes;

        public int FrustumPlanesCount;

        public IShadowMapProvider? ShadowMapProvider;

        public IBloomProvider? BloomProvider;

        public IMotionVectorProvider? MotionVectorProvider;

        public IDepthCullProvider? DepthCullProvider;

        public ShaderUpdate? LastGlobalUpdate;

        public long ContextVersion;

        public bool UseInstanceDraw;

        public bool UseMotionVectors;

        public bool IsSrgbTarget;

        public bool IsSrgbAutoEncode;

        public bool NeedSrgbEncode => IsSrgbTarget && !IsSrgbAutoEncode;

        public bool UseAngle;

        public bool UseCopyDepth;

        public bool UsePrimitiveBoundingBox;

        public Texture2D? CopyDepthImage;

        public Rect2I[]? ClipRegions;

        public bool IsMultiView;
    }

    public readonly struct ShaderUpdateBuilder : IFeatureList
    {
        private readonly ShaderUpdate _result;

        public delegate TValue UpdateAction<TValue>(UpdateShaderContext ctx);

        public ShaderUpdateBuilder(UpdateShaderContext context)
        {
            _result = new ShaderUpdate()
            {
                BufferUpdates = [],
            };

            Context = context;
        }

        readonly void Update<TValue>(UpdateAction<TValue> action, Action<IUniformProvider, TValue> doUpdate)
        {
            _result.Actions.Add((ctx, up) => doUpdate(up, action(ctx)));
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

        public readonly void LoadBuffer<T>(UpdateAction<T?> value, int slot,
            BufferStore store,
            BufferUsage usage = BufferUsage.Uniforms,
            string? uniformName = null) where T : struct
        {
            ISimpleBuffer<T>? buffer = null;

            _result.BufferUpdates ??= [];

            _result.BufferUpdates.Add((ctx) =>
            {
                if (store == BufferStore.Model || buffer == null)
                {
                    var curBuffer = ctx.BufferProvider?.GetBuffer<T>(slot, store, usage, uniformName);
#if DEBUG
                    if (buffer != null && curBuffer != buffer && store != BufferStore.Model)
                        XrEngine.Log.Warn(typeof(ShaderUpdateBuilder), "Buffer changed");
#endif
                    buffer = curBuffer;
                }

                Debug.Assert(buffer != null);

#if GL_WRAPPER
                buffer.Update(() =>
                {
                    ctx.CurrentBuffer = buffer;
                    var curValue = value(ctx);
                    ctx.CurrentBuffer = null;
                    return curValue;
                });
#else
                ctx.CurrentBuffer = buffer;

                var curValue = value(ctx);
                if (curValue != null)
                    buffer.Update(curValue.Value);

                ctx.CurrentBuffer = null;
#endif
            });

            _result.Actions.Add((ctx, up) =>
            {
#warning DANGER, THIS WORKS FOR MODEL STURE ONLY IF BufferUpdates IS RUNNING BEFORE THIS CALL
                if (buffer == null)
                    return;
                up.LoadBuffer(buffer, slot);
            });
        }

        public readonly void LoadBufferArray<T>(UpdateAction<T[]?> value, int slot, BufferStore store, BufferUsage usage = BufferUsage.Uniforms) where T : struct
        {
            ISimpleBuffer<T>? buffer = null;

            _result.BufferUpdates ??= [];

            _result.BufferUpdates.Add((ctx) =>
            {
                buffer = ctx.BufferProvider!.GetBuffer<T>(slot, store, usage);

                ctx.CurrentBuffer = buffer;

                var curValue = value(ctx);
                if (curValue != null && buffer is IBuffer<T> fullBuff)
                    fullBuff.UpdateRange(curValue, 0);

                ctx.CurrentBuffer = null;
            });

            _result.Actions.Add((ctx, up) =>
            {
                buffer = ctx.BufferProvider!.GetBuffer<T>(slot, store, usage);

                up.LoadBuffer(buffer, slot);
            });
        }

        public readonly void ExecuteAction(UpdateUniformAction action)
        {
            _result.Actions.Add(action);
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
            _result.Features ??= [];
            _result.Features.Add(name);
        }

        public readonly void AddFeature(string name, Func<UpdateShaderContext, bool> getValue, bool isDynamic = true)
        {
            if (!isDynamic)
            {
                if (getValue(Context))
                    AddFeature(name);
                return;
            }

            _result.DynamicFeatures ??= [];
            _result.DynamicFeatures.Add(name);

            ExecuteAction((ctx, up) =>
            {
                up.SetUniform(name, getValue(ctx));
            });
        }

        public readonly void AddExtension(string name)
        {
            _result.Extensions ??= [];
            _result.Extensions.Add(name);
        }

        public readonly void ComputeHash(string shaderId)
        {
            _result.FeaturesHash = HashBuilder.Instance.Compute(shaderId, _result.Features);
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
