
using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using static System.Formats.Asn1.AsnWriter;


namespace OpenXr.Engine.OpenGL
{
    public class GlobalContent
    {
        public DirectionalLight? Directional;
        
        public AmbientLight? Ambient;

        public PointLight? Point;

        public long Version;

        public Scene? Scene;
         
        public readonly Dictionary<Shader, ShaderContent> Contents = [];
    }

    public class ShaderContent
    {
        public GlProgram? Program;

        public readonly Dictionary<Geometry, VertexContent> Contents = [];
    }

    public class VertexContent
    {
        public GlVertexArray<VertexData, int>? VertexArray;

        public readonly List<DrawContent> Contents = [];
    }

    public class DrawContent
    {
        public Object3D? Object;

        public ShaderMaterial? Material;

        public Action? Draw;
    }

    public class OpenGLRender : IRenderEngine
    {
        protected IGLContext _context;
        protected GL _gl;
        protected Dictionary<uint, GlFrameTextureBuffer> _frameBuffers;
        protected GlFrameTextureBuffer? _frameBuffer;
        protected GlVertexLayout _meshLayout;
        protected GlobalContent _content;

        public static class Props
        {
            public const string GlResId = nameof(GlResId);    
        }

        public OpenGLRender(IGLContext context, GL gl)
        {
            _context = context;
            _gl = gl;
            _frameBuffers = new Dictionary<uint, GlFrameTextureBuffer>();
            _meshLayout = GlVertexLayout.FromType<VertexData>();
        }

        protected unsafe TGl GetResource<T, TGl>(T obj, Func<T, TGl> factory) where T : EngineObject where TGl :GlObject
        {
            var glObj = obj.GetProp<TGl?>(Props.GlResId);
            if (glObj == null)
            {
                glObj = factory(obj);
                obj.SetProp(Props.GlResId, glObj);
            }

            return glObj;
        }

        protected GlProgram GetProgram(Shader shader)
        {
            return GetResource(shader,
                a => new GlProgram(_gl, shader.VertexSource!, shader.FragmentSource!));
        }

        public void Clear()
        {
            _gl.ClearColor(0.7f, 0.7f, 0.7f, 0);
            _gl.ClearDepth(1.0f);
            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        }

        protected void Setup()
        {
            _gl.FrontFace(FrontFaceDirection.CW);
            _gl.CullFace(TriangleFace.Back);
            //_gl.Enable(EnableCap.CullFace);
            _gl.Enable(EnableCap.DepthTest);
        }

        protected void DrawMesh(Mesh mesh)
        {
            var vertexBuffer = GetResource(mesh.Geometry!, geo =>
                new GlVertexArray<VertexData, int>(_gl, geo.Vertices!, geo.Indices!, _meshLayout));

            vertexBuffer.Bind();

            _gl.DrawArrays(PrimitiveType.Triangles, 0, mesh.Geometry!.TriangleCount);

            vertexBuffer.Unbind();
        }

        [MemberNotNull(nameof(_content))]
        protected void BuildContent(Scene scene)
        {
            if (_content == null)
                _content = new GlobalContent();

            _content.Ambient = scene.VisibleDescendants<AmbientLight>().FirstOrDefault();
            _content.Point = scene.VisibleDescendants<PointLight>().FirstOrDefault();
            _content.Scene = scene;
            _content.Version = scene.Version;

            _content.Contents.Clear();

            foreach (var mesh in scene.VisibleDescendants<Mesh>())
            {
                if (mesh.Geometry == null || mesh.Materials.Count == 0)
                    continue;

                foreach (var material in mesh.Materials.OfType<ShaderMaterial>())
                {
                    if (material.Shader == null)
                        continue;

                    if (!_content.Contents.TryGetValue(material.Shader, out var shaderContent))
                    {
                        shaderContent = new ShaderContent();
                        shaderContent.Program = GetProgram(material.Shader);
                        _content.Contents[material.Shader] = shaderContent;
                    }
                    
                    if (!shaderContent.Contents.TryGetValue(mesh.Geometry, out var vertexContent))
                    {
                        vertexContent = new VertexContent();
                        vertexContent.VertexArray = GetResource(mesh.Geometry!, geo =>
                                new GlVertexArray<VertexData, int>(_gl, geo.Vertices!, geo.Indices!, _meshLayout));

                        shaderContent.Contents[mesh.Geometry] = vertexContent;
                    }

                    vertexContent.Contents.Add(new DrawContent
                    {
                        Draw = () => _gl.DrawArrays(PrimitiveType.Triangles, 0, mesh.Geometry!.TriangleCount),
                        Material = material,
                        Object = mesh
                    });
                }
            }

        }

        public void EnableDebug()
        {
             _gl.DebugMessageCallback((source, type, id, sev, len, msg, param) =>
            {
                unsafe
                {
                    var span = new Span<byte>((void*)msg, len);
                    var text = Encoding.UTF8.GetString(span);
                    Debug.WriteLine($"------ OPENGL: {text}");
                }
     

            }, 0);

            _gl.Enable(EnableCap.DebugOutput);
        }

        public void Render(Scene scene, Camera camera, RectI view)
        {
            if (_frameBuffer != null)
                _frameBuffer.Bind();
            
            Clear();
            
            Setup();

            _gl.Viewport(view.X, view.Y, view.Width, view.Height);

            if (_content == null || _content.Scene != scene || _content.Version != scene.Version)
                BuildContent(scene);
        
            foreach (var shader in _content.Contents.Values)
            {
                var prog = shader.Program!;

                prog!.Use();

                if (_content.Ambient != null)
                    prog.SetUniform("light.ambient", (Vector3)_content.Ambient.Color * _content.Ambient.Intensity);

                if (_content.Point != null)
                {
                    var wordPos = Vector3.Transform(_content.Point.Transform.Position, _content.Point.WorldMatrix);

                    prog.SetUniform("light.diffuse", (Vector3)_content.Point.Color * _content.Point.Intensity);
                    prog.SetUniform("light.position", wordPos);
                    prog.SetUniform("light.specular", _content.Point.Specular);
                }

                prog.SetUniform("uView", camera.Transform.Matrix);
                prog.SetUniform("uProjection", camera.Projection);
                prog.SetUniform("viewPos", camera.Transform.Position);

                foreach (var vertex in shader.Contents.Values)
                {
                    vertex.VertexArray!.Bind();

                    foreach (var draw in vertex.Contents)
                    {
                        draw.Material!.UpdateUniforms(prog);
                        prog.SetUniform("uModel", draw.Object!.WorldMatrix);
                        draw.Draw!();
                    }

                    vertex.VertexArray.Unbind();
                }

                prog.Unbind();
            }


            if (_frameBuffer != null)
            {
                //_frameBuffer.InvalidateDepth();
                _frameBuffer.Unbind();
            }

        }

        public void SetImageTarget(uint image)
        {
            if (!_frameBuffers.TryGetValue(image, out var frameBuffer))
            {
                frameBuffer = new GlFrameTextureBuffer(_gl, new GlTexture2D(_gl, image));
               _frameBuffers[image] = frameBuffer;
            }

            _frameBuffer = frameBuffer; 
        }

        public void Dispose()
        {
        }
    }
}
