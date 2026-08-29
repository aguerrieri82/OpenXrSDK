#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;
using Buffer = System.Buffer;

namespace XrEngine.OpenGL
{
    public abstract class GlVertexSourceHandle : IDisposable
    {
        protected static Dictionary<string, GlVertexLayout> _layouts = [];

        public abstract void Unbind();

        public abstract void Bind();

        public abstract void Update();

        public abstract void DrawInstances(int count, DrawPrimitive? forcePrimitive = null);

        public abstract void Draw(DrawPrimitive? forcePrimitive = null);

        public abstract void Dispose();

        public abstract GlVertexLayout Layout { get; }

        public abstract bool NeedUpdate { get; }

        public abstract IVertexSource Source { get; }

        public long Version { get; protected set; }

        public abstract IGlVertexArray VertexArray { get; }

        public abstract GlVertexSourceHandle Clone();

        public static GlVertexSourceHandle Create(GL gl, IVertexSource obj)
        {
            var srcInterface = obj.GetType().GetInterfaces()
                .First(a => a.IsGenericType && a.GetGenericTypeDefinition() == typeof(IVertexSource<,>));

            var srcTypes = srcInterface.GetGenericArguments();

            if (obj is ICompressedVertexSource comp && (comp.CompVertexType != null || comp.CompIndexType != null))
            {
                var compVertType = comp.CompVertexType ?? srcTypes[0];
                var compIndexType = comp.CompIndexType ?? srcTypes[1];

                var type = typeof(GlCompressedVertexSourceHandler<,,,>).MakeGenericType(srcTypes[0], srcTypes[1], compVertType, compIndexType);

                return (GlVertexSourceHandle)Activator.CreateInstance(type, [gl, obj])!;
            }

            var srcType = typeof(GlVertexSourceHandler<,>).MakeGenericType(srcTypes);

            return (GlVertexSourceHandle)Activator.CreateInstance(srcType, [gl, obj])!;
        }

    }

    public class GlVertexSourceHandler<TVert, TInd> : GlVertexSourceHandle where TVert : unmanaged where TInd : unmanaged
    {
        readonly GlVertexArray<TVert, TInd> _vertices;
        readonly PrimitiveType _primitive;
        readonly IVertexSource<TVert, TInd> _source;
        readonly GL _gl;
        EngineObject? _sourceObject;
        VertexComponent _lastComponents;

        public GlVertexSourceHandler(GlVertexSourceHandler<TVert, TInd> source)
        {
            _source = source._source;

            _vertices = new GlVertexArray<TVert, TInd>(source._gl, source._vertices.VBuf, source._vertices.IBuf, source._vertices.MainLayout);

            _primitive = source._primitive;

            if (source._vertices.Attributes != null)
            {
                foreach (var attr in source._vertices.Attributes)
                    _vertices.AddAttributes(attr.Buffer!, attr.Layout!, attr.ElementType!);
            }

            _gl = source._gl;

            Version = source.Version;
        }

        public GlVertexSourceHandler(GL gl, IVertexSource<TVert, TInd> source)
        {
            _source = source;

            UpdateMainLayout(out var mainLayout);

            _vertices = new GlVertexArray<TVert, TInd>(gl, _source.Vertices, _source.Indices, mainLayout!);

            _primitive = GlPrimitive(_source.Primitive);

            _source.NotifyBuffers(_vertices.VBuf, _vertices.IBuf);

            _gl = gl;

            foreach (var attrs in _source.Host.Components<IVertexAttributes>())
            {
                var attrLen = attrs.BufferCount;

                for (var i = 0; i < attrLen; i++)
                {
                    var attrBuffer = attrs.GetBuffer(i);

                    var elementType = attrBuffer.ElementType ?? attrBuffer.Data.GetType().GetElementType()!;

                    var glBuffer = GlBuffer.Create(_gl, BufferTargetARB.ArrayBuffer, elementType);

                    var layout = CreateLayout(elementType, attrBuffer.BaseLocation, attrBuffer.Component);

                    _vertices.AddAttributes(glBuffer, layout, elementType);
                }
            }

            Version = -1;
        }

        public override GlVertexSourceHandle Clone()
        {
            return (GlVertexSourceHandle)Activator.CreateInstance(GetType(), this)!;
        }

        protected GlVertexLayout CreateLayout(Type type, uint baseLocation = 0, VertexComponent component = VertexComponent.None)
        {
            var lKey = string.Concat(type.FullName, _source.ActiveComponents);

            if (!_layouts.TryGetValue(lKey, out var layout))
            {
                layout = GlVertexLayout.FromType(type, _source.ActiveComponents, baseLocation);

                if (component != VertexComponent.None)
                {
                    Debug.Assert(layout.Attributes != null);

                    for (var j = 0; j < layout.Attributes.Length; j++)
                        layout.Attributes[j].Component = component;
                }

                _layouts[lKey] = layout;
            }

            return layout;
        }

        protected bool UpdateMainLayout(out GlVertexLayout? layout)
        {
            if (_lastComponents == _source.ActiveComponents)
            {
                layout = null;
                return false;
            }

            layout = CreateLayout(typeof(TVert));

            _lastComponents = _source.ActiveComponents;

            return true;
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
                DrawPrimitive.Quad => PrimitiveType.Quads,

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

        public override void DrawInstances(int count, DrawPrimitive? forcePrimitive = null)
        {
            _vertices.DrawInstances(forcePrimitive != null ? GlPrimitive(forcePrimitive.Value) : _primitive, count);
        }

        public override void Draw(DrawPrimitive? forcePrimitive = null)
        {
            if (_source.InstanceCount > 1)
            {
                DrawInstances(_source.InstanceCount, forcePrimitive);
            }
            else
                _vertices.Draw(forcePrimitive != null ? GlPrimitive(forcePrimitive.Value) : _primitive);
        }

        public override void Update()
        {
            if ((_source.Host.Flags & EngineObjectFlags.NoLogs) == 0)
                _vertices.EnableDebug = true;

            if (UpdateMainLayout(out var layout))
                _vertices.UpdateMainLayouts(layout!);

            _vertices.UpdateMain(_source.Vertices, _source.Indices);

            _sourceObject = _source.Host;

            foreach (var attrs in _source.Host.Components<IVertexAttributes>())
            {
                for (var i = 0; i < attrs.BufferCount; i++)
                {
                    var attrBuffer = attrs.GetBuffer(i);
                    _vertices.UpdateAttributes(attrBuffer.Data, i);
                }
            }

            Version = _sourceObject.Version;

            _source.NotifyLoaded();
        }

        public override void Dispose()
        {
            _vertices.Dispose();

            GC.SuppressFinalize(this);
        }

        public override IGlVertexArray VertexArray => _vertices;

        public override IVertexSource Source => _source;

        public override bool NeedUpdate => _source.Host != null &&
                            (_source.Host.Version != Version || Version == -1 || _sourceObject != _source.Host);

        public override GlVertexLayout Layout => _vertices.MainLayout;
    }

    public class GlCompressedVertexSourceHandler<TVert, TInd, TCompVert, TCompInd> : GlVertexSourceHandle
        where TVert : unmanaged
        where TInd : unmanaged
        where TCompVert : unmanaged
        where TCompInd : unmanaged
    {
        readonly GlVertexArray<TCompVert, TCompInd> _vertices;
        readonly PrimitiveType _primitive;
        readonly IVertexSource<TVert, TInd> _source;
        readonly ICompressedVertexSource _compSource;
        readonly GL _gl;
        EngineObject? _sourceObject;
        VertexComponent _lastComponents;

        public GlCompressedVertexSourceHandler(GlCompressedVertexSourceHandler<TVert, TInd, TCompVert, TCompInd> source)
        {
            _source = source._source;
            _compSource = source._compSource;

            _vertices = new GlVertexArray<TCompVert, TCompInd>(source._gl, source._vertices.VBuf, source._vertices.IBuf, source._vertices.MainLayout);

            _primitive = source._primitive;

            if (source._vertices.Attributes != null)
            {
                foreach (var attr in source._vertices.Attributes)
                    _vertices.AddAttributes(attr.Buffer!, attr.Layout!, attr.ElementType!);
            }

            _gl = source._gl;

            Version = source.Version;
        }

        public GlCompressedVertexSourceHandler(GL gl, IVertexSource<TVert, TInd> source)
        {
            _source = source;
            _compSource = (ICompressedVertexSource)source;

            UpdateMainLayout(out var mainLayout);

            var vBuf = new GlBuffer<TCompVert>(gl, BufferTargetARB.ArrayBuffer);
            GlBuffer<TCompInd>? iBuf = null;

            if (_source.Indices != null)
                iBuf = new GlBuffer<TCompInd>(gl, BufferTargetARB.ElementArrayBuffer);

            _vertices = new GlVertexArray<TCompVert, TCompInd>(gl, vBuf, iBuf, mainLayout!);

            _primitive = GlPrimitive(_source.Primitive);

            //_source.NotifyBuffers(_vertices.VBuf, _vertices.IBuf);

            _gl = gl;

            foreach (var attrs in _source.Host.Components<IVertexAttributes>())
            {
                var attrLen = attrs.BufferCount;

                for (var i = 0; i < attrLen; i++)
                {
                    var attrBuffer = attrs.GetBuffer(i);

                    var elementType = attrBuffer.ElementType ?? attrBuffer.Data.GetType().GetElementType()!;

                    var glBuffer = GlBuffer.Create(_gl, BufferTargetARB.ArrayBuffer, elementType);

                    var layout = CreateLayout(elementType, attrBuffer.BaseLocation, attrBuffer.Component);

                    _vertices.AddAttributes(glBuffer, layout, elementType);
                }
            }

            Version = -1;
        }

        unsafe void UpdateMain()
        {
            var vertexCount = _source.Vertices.Length;
            var vertexSize = (uint)(vertexCount * sizeof(TCompVert));

            if (_vertices.VBuf.SizeBytes != vertexSize)
                _vertices.VBuf.Allocate(vertexSize);

            var pVertices = _vertices.VBuf.Map(MapBufferAccessMask.WriteBit | MapBufferAccessMask.InvalidateBufferBit);

            fixed (TVert* pSrc = _source.Vertices)
            {
                if (_compSource.CompVertexType != null)
                    _compSource.CompressVertices(pSrc, pVertices.Data, vertexCount);
                else
                    Buffer.MemoryCopy(pSrc, pVertices.Data, vertexSize, vertexCount * sizeof(TVert));
            }

            _vertices.VBuf.Unmap();
            _vertices.VBuf.ArrayLength = (uint)vertexCount;

            if (_source.Indices == null)
                return;

            var indexCount = _source.Indices.Length;
            var indexSize = (uint)(indexCount * sizeof(TCompInd));

            if (_vertices.IBuf!.SizeBytes != indexSize)
                _vertices.IBuf.Allocate(indexSize);

            var pIndices = _vertices.IBuf.Map(MapBufferAccessMask.WriteBit | MapBufferAccessMask.InvalidateBufferBit);

            fixed (TInd* pSrc = _source.Indices)
            {
                if (_compSource.CompIndexType != null)
                    _compSource.CompressIndices(pSrc, pIndices.Data, indexCount);
                else
                    Buffer.MemoryCopy(pSrc, pIndices.Data, indexSize, indexCount * sizeof(TInd));
            }

            _vertices.IBuf.Unmap();
            _vertices.IBuf.ArrayLength = (uint)indexCount;
        }

        public override GlVertexSourceHandle Clone()
        {
            return (GlVertexSourceHandle)Activator.CreateInstance(GetType(), this)!;
        }

        protected GlVertexLayout CreateLayout(Type type, uint baseLocation = 0, VertexComponent component = VertexComponent.None)
        {
            var lKey = string.Concat(type.FullName, _source.ActiveComponents);

            if (!_layouts.TryGetValue(lKey, out var layout))
            {
                layout = GlVertexLayout.FromType(type, _source.ActiveComponents, baseLocation);

                if (component != VertexComponent.None)
                {
                    Debug.Assert(layout.Attributes != null);

                    for (var j = 0; j < layout.Attributes.Length; j++)
                        layout.Attributes[j].Component = component;
                }

                _layouts[lKey] = layout;
            }

            return layout;
        }

        protected bool UpdateMainLayout(out GlVertexLayout? layout)
        {
            if (_lastComponents == _source.ActiveComponents)
            {
                layout = null;
                return false;
            }

            layout = CreateLayout(typeof(TCompVert));

            _lastComponents = _source.ActiveComponents;

            return true;
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
                DrawPrimitive.Quad => PrimitiveType.Quads,

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

        public override void DrawInstances(int count, DrawPrimitive? forcePrimitive = null)
        {
            _vertices.DrawInstances(forcePrimitive != null ? GlPrimitive(forcePrimitive.Value) : _primitive, count);
        }

        public override void Draw(DrawPrimitive? forcePrimitive = null)
        {
            if (_source.InstanceCount > 1)
            {
                DrawInstances(_source.InstanceCount, forcePrimitive);
            }
            else
                _vertices.Draw(forcePrimitive != null ? GlPrimitive(forcePrimitive.Value) : _primitive);
        }

        public override void Update()
        {
            if ((_source.Host.Flags & EngineObjectFlags.NoLogs) == 0)
                _vertices.EnableDebug = true;

            if (UpdateMainLayout(out var layout))
                _vertices.UpdateMainLayouts(layout!);

            UpdateMain();

            _sourceObject = _source.Host;

            foreach (var attrs in _source.Host.Components<IVertexAttributes>())
            {
                for (var i = 0; i < attrs.BufferCount; i++)
                {
                    var attrBuffer = attrs.GetBuffer(i);
                    _vertices.UpdateAttributes(attrBuffer.Data, i);
                }
            }

            Version = _sourceObject.Version;

            _source.NotifyLoaded();
        }

        public override void Dispose()
        {
            _vertices.Dispose();

            GC.SuppressFinalize(this);
        }

        public override IGlVertexArray VertexArray => _vertices;

        public override IVertexSource Source => _source;

        public override bool NeedUpdate => _source.Host != null &&
                            (_source.Host.Version != Version || Version == -1 || _sourceObject != _source.Host);

        public override GlVertexLayout Layout => _vertices.MainLayout;
    }
}
