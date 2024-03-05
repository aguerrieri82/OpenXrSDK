#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif



namespace Xr.Engine.OpenGL
{
    public abstract class GlVertexSourceHandle : IDisposable
    {
        protected static Dictionary<string, GlVertexLayout> _layouts = [];

        public abstract void Unbind();

        public abstract void Bind();

        public abstract void Update();

        public abstract void Draw();

        public abstract GlVertexLayout Layout { get; }

        public static GlVertexSourceHandle Create(GL gl, IVertexSource obj)
        {
            var srcInterface = obj.GetType().GetInterfaces()
                .First(a => a.IsGenericType && a.GetGenericTypeDefinition() == typeof(IVertexSource<,>));

            var type = typeof(GlVertexSourceHandler<,>).MakeGenericType(srcInterface.GetGenericArguments());

            return (GlVertexSourceHandle)Activator.CreateInstance(type, [gl, obj])!;
        }

        public abstract void Dispose();
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

            _primitive = _source.Primitive switch
            {
                DrawPrimitive.Triangle => PrimitiveType.Triangles,
                DrawPrimitive.Line => PrimitiveType.Lines,
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

        public override void Draw()
        {
            _vertices.Draw(_primitive);
        }

        public override void Update()
        {
            _vertices.Update(_source.Vertices, _source.Indices);
        }

        public override void Dispose()
        {
            _vertices.Dispose();
            GC.SuppressFinalize(this);
        }

        public override GlVertexLayout Layout => _vertices.Layout;
    }
}
