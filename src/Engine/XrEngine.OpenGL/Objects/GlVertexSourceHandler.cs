#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public abstract class GlVertexSourceHandle : IDisposable
    {
        protected static Dictionary<string, GlVertexLayout> _layouts = [];

        public abstract void Unbind();

        public abstract void Bind();

        public abstract void Update();

        public abstract void Draw(DrawPrimitive? forcePrimitive = null);

        public abstract void Dispose();

        public abstract GlVertexLayout Layout { get; }

        public abstract bool NeedUpdate { get; }

        public abstract IVertexSource Source { get; }

        public long Version { get; protected set; }

        public static GlVertexSourceHandle Create(GL gl, IVertexSource obj)
        {
            var srcInterface = obj.GetType().GetInterfaces()
                .First(a => a.IsGenericType && a.GetGenericTypeDefinition() == typeof(IVertexSource<,>));

            var type = typeof(GlVertexSourceHandler<,>).MakeGenericType(srcInterface.GetGenericArguments());

            return (GlVertexSourceHandle)Activator.CreateInstance(type, [gl, obj])!;
        }
    }

    public class GlVertexSourceHandler<TVert, TInd> : GlVertexSourceHandle where TVert : unmanaged where TInd : unmanaged
    {
        readonly GlVertexArray<TVert, TInd> _vertices;
        readonly PrimitiveType _primitive;
        readonly IVertexSource<TVert, TInd> _source;

        public GlVertexSourceHandler(GL gl, IVertexSource<TVert, TInd> source)
        {
            var lKey = string.Concat(typeof(TVert).FullName, source.ActiveComponents);

            if (!_layouts.TryGetValue(lKey, out var layout))
            {
                layout = GlVertexLayout.FromType<TVert>(source.ActiveComponents);
                _layouts[lKey] = layout;
            }

            _source = source;
            _vertices = new GlVertexArray<TVert, TInd>(gl, _source.Vertices, _source.Indices, layout);

            _primitive = GlPrimitive(_source.Primitive);

            Version = -1;
        }

        static PrimitiveType GlPrimitive(DrawPrimitive drawPrimitive)
        {
            return drawPrimitive switch
            {
                DrawPrimitive.Triangle => PrimitiveType.Triangles,
                DrawPrimitive.Line => PrimitiveType.Lines,
                DrawPrimitive.LineLoop => PrimitiveType.LineLoop,
                DrawPrimitive.Point => PrimitiveType.Points,
                DrawPrimitive.Patch => PrimitiveType.Patches,

                _ => throw new NotSupportedException()
            };
        }

        public override void Bind()
        {
            _vertices.Bind();
        }

        public override void Unbind()
        {
            _vertices.Unbind();
        }

        public override void Draw(DrawPrimitive? forcePrimitive = null)
        {
            _vertices.Draw(forcePrimitive != null ? GlPrimitive(forcePrimitive.Value) : _primitive);
        }

        public override void Update()
        {
            _vertices.Update(_source.Vertices, _source.Indices);

            Version = _source.Object.Version;

            _source.NotifyLoaded();
        }

        public override void Dispose()
        {
            _vertices.Dispose();

            GC.SuppressFinalize(this);
        }

        public override IVertexSource Source => _source;

        public override bool NeedUpdate => _source.Object != null && (_source.Object.Version != Version || Version == -1);

        public override GlVertexLayout Layout => _vertices.Layout;
    }
}
