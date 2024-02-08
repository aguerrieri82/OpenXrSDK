using OpenXr.Engine.Object;
using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Silk.NET.Core.Native.WinString;
using static System.Net.Mime.MediaTypeNames;

namespace OpenXr.Engine.OpenGL
{
    public class OpenGLRender : IRenderEngine
    {
        protected IGLContext _context;
        protected GL _gl;
        protected uint _imageBuffer;
        protected Dictionary<uint, uint> _dephBufferBind;
        protected uint _frameBuffer;

        public static class Props
        {
            public const string GlResId = nameof(GlResId);    
        }

        public OpenGLRender(IGLContext context, GL gl)
        {
            _context = context;
            _gl = gl;
            _dephBufferBind = new Dictionary<uint, uint>();

        }

        public void Dispose()
        {
        }

        public void Initialize()
        {
            _frameBuffer = _gl.CreateFramebuffer();
        }

        protected unsafe uint GetResource<T>(T obj, Func<T, uint> factory) where T : EngineObject
        {
            var resId = obj.GetProp<uint>(Props.GlResId);
            if (resId == 0)
            {
                resId = factory(obj);
                obj.SetProp(Props.GlResId, resId);
            }

            return resId;
        }


        protected uint BuildProgram(string vertexShader, string fragmentShader)
        {
            var vs = BuildShader(ShaderType.VertexShader, vertexShader);
            var fs = BuildShader(ShaderType.FragmentShader, fragmentShader);

            return BuildProgram(vs, fs);
        }

        protected uint BuildProgram(params uint[] shaders)
        {
            var prog = _gl.CreateProgram();

            foreach (var shader in shaders)
                _gl.AttachShader(prog, shader);

            _gl.LinkProgram(prog);

            if (_gl.GetProgram(prog, ProgramPropertyARB.LinkStatus) == 0)
            {
                var log = _gl.GetProgramInfoLog(prog);
                throw new Exception(log);
            }

            foreach (var shader in shaders)
                _gl.DeleteShader(shader);

            return prog;
        }

        protected uint BuildShader(ShaderType type, string source)
        {
            var result = _gl.CreateShader(type);
            _gl.ShaderSource(result, source);
            _gl.CompileShader(result);

            
            if (_gl.GetShader(result, ShaderParameterName.CompileStatus) == 0)
            {
                var log = _gl.GetShaderInfoLog(result);
                throw new Exception(log);   
            }

            return result;
        }

        protected uint GetProgram(Shader shader)
        {
            return GetResource(shader, s => BuildProgram(s.VertexSource!, s.FragmentSource!));
        }

        protected unsafe uint CreateTexture(Texture2D texture)
        {
            InternalFormat internalFormat;

            switch (texture.Format)
            {
                case TextureFormat.Deph32Float:
                    internalFormat = InternalFormat.DepthComponent32;
                    break;
                default:
                    throw new NotSupportedException();
            }

            PixelFormat pixelFormat;

            switch (texture.Format)
            {
                case TextureFormat.Deph32Float:
                    pixelFormat = PixelFormat.DepthComponent;
                    break;
                default:
                    throw new NotSupportedException();
            }

            PixelType pixelType;

            switch (texture.Format)
            {
                case TextureFormat.Deph32Float:
                    pixelType = PixelType.Float;
                    break;
                default:
                    throw new NotSupportedException();
            }

            var tex = _gl.GenTexture();

            _gl.BindTexture(TextureTarget.Texture2D, tex);

            _gl.TextureParameter(tex, TextureParameterName.TextureWrapS, (uint)texture.WrapS);
            _gl.TextureParameter(tex, TextureParameterName.TextureWrapT, (uint)texture.WrapT);
            _gl.TextureParameter(tex, TextureParameterName.TextureMagFilter, (uint)texture.MagFilter);
            _gl.TextureParameter(tex, TextureParameterName.TextureMinFilter, (uint)texture.MinFilter);

            _gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                internalFormat,
                texture.Width,
                texture.Height,
                0,
                pixelFormat,
                pixelType,
                null);

            _gl.BindTexture(TextureTarget.Texture2D, 0);

            return tex;
        }


        protected unsafe uint GetTexture(Texture2D texture)
        {
            return GetResource(texture, CreateTexture);
        }

        public void Render(Scene scene, Camera camera, RectI view)
        {
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _frameBuffer);

            _gl.Viewport(view.X, view.Y, view.Width, view.Height);

            _gl.FrontFace(FrontFaceDirection.CW);
            _gl.CullFace(TriangleFace.Back);
            _gl.Enable(EnableCap.CullFace);
            _gl.Enable(EnableCap.DepthTest);

            _gl.ClearColor(0, 0, 0, 0);
            _gl.ClearDepth(1.0f);
            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);


            if (_imageBuffer != 0)
            {
                _gl.FramebufferTexture2D(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.ColorAttachment0,
                    TextureTarget.Texture2D,
                    _imageBuffer, 0);

                uint dephImage;

                if (!_dephBufferBind.TryGetValue(_imageBuffer, out dephImage))
                {
                    var texture = new Texture2D()
                    {
                        Format = TextureFormat.Deph32Float,
                        WrapS = WrapMode.ClampToEdge,
                        WrapT = WrapMode.ClampToEdge,
                        MagFilter = ScaleFilter.Nearest,
                        MinFilter = ScaleFilter.Nearest,
                        Width = view.Width,
                        Height = view.Height
                    };

                    dephImage = GetTexture(texture);

                    _dephBufferBind[_imageBuffer] = GetTexture(texture);
                }

                _gl.FramebufferTexture2D(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.DepthAttachment,
                    TextureTarget.Texture2D,
                    dephImage,
                  0);
            }

            var ambient = scene.VisibleDescendants<AmbientLight>().FirstOrDefault();

            var directional = scene.VisibleDescendants<DirectionalLight>().FirstOrDefault();

            foreach (var mesh in scene.VisibleDescendants<Mesh>())
            {
                if (mesh.Materials == null)
                    continue;

                foreach (var material in mesh.Materials)
                {
                    if (material is ShaderMaterial shaderMat)
                    {
                        var prog = GetProgram(shaderMat.Shader!);
                        
                        _gl.UseProgram(prog);
                    }
                }
            }

    
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            _gl.UseProgram(0);
            _gl.BindVertexArray(0); 
        }

        public void SetImageTarget(uint image)
        {
            _imageBuffer = image;
        }

    }
}
