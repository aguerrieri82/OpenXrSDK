
using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;


namespace OpenXr.Engine.OpenGL
{
    public class OpenGLRender : IRenderEngine
    {
        protected IGLContext _context;
        protected GL _gl;
        protected Dictionary<uint, GlFrameTextureBuffer> _frameBuffers;
        protected GlFrameBuffer? _frameBuffer;

        public static class Props
        {
            public const string GlResId = nameof(GlResId);    
        }

        public OpenGLRender(IGLContext context, GL gl)
        {
            _context = context;
            _gl = gl;
            _frameBuffers = new Dictionary<uint, GlFrameTextureBuffer>();

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
            _gl.ClearColor(0, 0, 0, 0);
            _gl.ClearDepth(1.0f);
            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        }

        protected void Setup()
        {
            _gl.FrontFace(FrontFaceDirection.CW);
            _gl.CullFace(TriangleFace.Back);
            _gl.Enable(EnableCap.CullFace);
            _gl.Enable(EnableCap.DepthTest);

        }

        public void Render(Scene scene, Camera camera, RectI view)
        {
            _frameBuffer!.Bind();
            
            Clear();
            
            Setup();

            _gl.Viewport(view.X, view.Y, view.Width, view.Height);

            var ambient = scene.VisibleDescendants<AmbientLight>().FirstOrDefault();

            var directional = scene.VisibleDescendants<DirectionalLight>().FirstOrDefault();

            var meshes = scene.VisibleDescendants<Mesh>()
                              .Where(a=> a.Materials != null);

            var shaders = meshes.SelectMany(a => a.Materials!.OfType<ShaderMaterial>().Select(a => a.Shader!))
                                .Distinct();

            foreach (var shader in shaders)
            {
                var prog = GetProgram(shader!);

                prog.Use();

                prog.SetUniform("uView", camera.Transform.Matrix);
                prog.SetUniform("uProjection", camera.Projection);
                prog.SetUniform("viewPos", camera.Transform.Position);

                prog.SetUniform("material.ambient", new Vector3(1.0f, 0.5f, 0.31f));
                prog.SetUniform("material.diffuse", new Vector3(1.0f, 0.5f, 0.31f));
                prog.SetUniform("material.specular", new Vector3(0.5f, 0.5f, 0.5f));
                prog.SetUniform("material.shininess", 32.0f);


                var shaderMeshes = meshes.Where(a =>
                    a.Materials!.OfType<ShaderMaterial>().Any(b => b.Shader == shader));

                foreach (var mesh in shaderMeshes)
                {
                    var materials = mesh.Materials!
                        .OfType<ShaderMaterial>()
                        .Where(b => b.Shader == shader);

                    prog.SetUniform("uModel", mesh.WorldMatrix);

                    foreach (var material in materials)
                    {
                        prog.SetUniform("material.ambient", material.Color);
                    }



                }
            }
        }

        public void SetImageTarget(uint image)
        {
            if (_frameBuffers.TryGetValue(image, out var frameBuffer))
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
