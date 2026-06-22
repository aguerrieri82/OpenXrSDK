#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using Common.Interop;
using XrMath;
using System.Diagnostics;


namespace XrEngine.OpenGL
{
    public static class GlImageProc
    {
        static readonly Dictionary<string, GlSimpleProgram> _programs = [];
        static uint _emptyVertexArray;
        static GlTextureFrameBuffer? _frameBuffer;
        static GlTextureFrameBuffer? _texReadFb;

        public static GlSimpleProgram LoadProgram(GL gl, string fragmentSource, string vertexSource)
        {
            return LoadProgram(gl, fragmentSource, vertexSource, [], []);
        }

        public static GlSimpleProgram LoadProgram(GL gl, string fragmentSource, string[] features, string[] extensions)
        {
            return LoadProgram(gl, fragmentSource, "fullscreen.vert", features, extensions); 
        }

        public static GlSimpleProgram LoadProgram(GL gl, string fragmentSource, string vertexSource, string[] features, string[] extensions)
        {
            var key = vertexSource + "_" + fragmentSource;

            if (!_programs.TryGetValue(key, out var program))
            {
                program = new GlSimpleProgram(gl, vertexSource, fragmentSource, str => Embedded.GetString<Material>(str));
                
                foreach (var ext in extensions.Where(a => !string.IsNullOrWhiteSpace(a)))
                    program.AddExtension(ext);

                foreach (var feature in features.Where(a=> !string.IsNullOrWhiteSpace(a)))
                    program.AddFeature(feature);
                
                program.Build();

                _programs[key] = program;
            }
            program.Use();
            return program;
        }

        public static GlTextureFrameBuffer PrepareFrameBuffer(GL gl, GlTexture? color = null, IGlRenderAttachment? depth = null)
        {
            _frameBuffer ??= GlTempAllocator.FrameBuffer(gl);
            _frameBuffer.Configure(color, depth, 1);
            _frameBuffer.Bind();
            return _frameBuffer;
        }

        public static GlTextureFrameBuffer PrepareFrameBuffer(GL gl, GlTexture? color, uint colorIndex)
        {
            _frameBuffer ??= GlTempAllocator.FrameBuffer(gl);
            _frameBuffer.Configure(color, colorIndex, null, 0, 1);
            _frameBuffer.Bind();
            return _frameBuffer;
        }

        public static void DrawVirtual(GL gl, uint vertices)
        {
            if (_emptyVertexArray == 0)
                _emptyVertexArray = gl.GenVertexArray();

            GlState.Current!.BindVertexArray(_emptyVertexArray);
            gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
        }


        public static void DrawGeometry(GL gl, Geometry3D geo, Texture2D srcImge, GlTexture dstImage, string fragName)
        {
            EngineNativeLib.RdcStartFrameCapture();

            PrepareFrameBuffer(gl, dstImage);

            var prog = LoadProgram(gl, fragName, "image_proc.vert");

            prog.Use();
            prog.LoadTexture(srcImge, 0);

            var mesh = new TriangleMesh(geo);

            using var vs = new GlVertexSourceHandler<VertexData, uint>(gl, mesh);
            vs.Update();

            GlState.Current!.SetView(new Rect2I
            {
                Width = dstImage.Width,
                Height = dstImage.Height
            });

            GlState.Current.SetAlphaMode(AlphaMode.Opaque);
            GlState.Current.SetWriteDepth(false);
            GlState.Current.SetUseDepth(false);
            GlState.Current.SetWriteColor(true);

            vs.Bind();
            vs.Draw();

            EngineNativeLib.RdcEndFrameCapture(false);
        }

        public static void DrawQuad(GL gl)
        {
            DrawVirtual(gl, 3);
        }

        public static void CopyDepth(IGlFrameBuffer src, GlTexture dst)
        {
            CopyDepth((GlTexture)src.Depth!, dst);
            src.Bind();
        }

        public static void CopyDepth(GlTexture src, GlTexture dst)
        {
            var prog = LoadProgram(src.GL, "copy_red.frag", src.Depth > 1 ? ["TEXTURE_ARRAY"] : [], []);

            GlState.Current!.SetView(new Rect2I(0, 0, src.Width, src.Height));
            GlState.Current!.SetWriteDepth(false);
            GlState.Current!.SetUseDepth(false);
            GlState.Current!.SetColorMask(true, false, false, false);

            GlState.Current!.LoadTexture(src, 0);

            if (src.Depth > 1)
            {
                Debug.Assert(src.Depth == dst.Depth);

                for (uint i = 0; i < src.Depth; i++)
                {
                    prog.SetUniform("uIndex", (int)i);
                    PrepareFrameBuffer(src.GL, dst, i);
                    DrawQuad(src.GL);
                }
            }
            else
            {
                PrepareFrameBuffer(src.GL, dst);
                DrawQuad(src.GL);
            }
        }

        public static void CopyColor(GlTexture src, GlTexture dst)
        {
            var GL_TEXTURE_EXTERNAL_OES =  (TextureTarget)0x8D65; // 36197

            var prog = LoadProgram(src.GL, "texture_full.frag", 
                src.Target == GL_TEXTURE_EXTERNAL_OES ? ["EXTERNAL"] : [],
                src.Target == GL_TEXTURE_EXTERNAL_OES ? ["GL_OES_EGL_image_external_essl3 "] : []
             );

            GlState.Current!.SetView(new Rect2I(0, 0, src.Width, src.Height));
            GlState.Current!.SetWriteDepth(false);
            GlState.Current!.SetUseDepth(false);
            GlState.Current!.SetWriteColor(true);

            GlState.Current!.LoadTexture(src, 0);

            PrepareFrameBuffer(src.GL, dst);

            DrawQuad(src.GL);
        }

        public unsafe static IList<TextureData>? Read(this GlTexture src,
            TextureFormat format,
            uint startMipLevel = 0,
            uint? endMipLevel = null,
            IList<IMemoryBuffer<byte>>? buffers = null)
        {
            var internalFormat = src.InternalFormat;

            var gl = src.GL;

            if (internalFormat.IsDepth())
            {
                var tmp = GlTempAllocator.StaticTexture(
                    src.GL,
                    src.Width,
                    src.Height,
                    src.Depth,
                    format);

                CopyDepth(src, tmp);

                return Read(tmp, format, 0, 0, buffers);
            }

            var result = new List<TextureData>();

            var attachment = FramebufferAttachment.ColorAttachment0;

            void ReadTarget(TextureTarget target, uint mipLevel, uint face = 0, uint depth = 0)
            {
                if (target == TextureTarget.Texture2DArray)
                {
                    gl.FramebufferTextureLayer(
                         FramebufferTarget.ReadFramebuffer,
                         attachment,
                         src.Handle,
                         (int)mipLevel,
                         (int)depth);
                }
                else
                {
                    gl.FramebufferTexture2D(
                         FramebufferTarget.ReadFramebuffer,
                         attachment,
                         target,
                         src.Handle,
                         (int)mipLevel);
                }

                var status = src.GL.CheckFramebufferStatus(FramebufferTarget.ReadFramebuffer);
                
                if (status != GLEnum.FramebufferComplete)
                    throw new Exception($"Framebuffer incomplete at mip {mipLevel}: {status}");

                var w = src.Width >> (int)mipLevel;
                var h = src.Height >> (int)mipLevel;

                GlState.Current!.SetView(new Rect2I(0, 0, w, h));

                var pixelSize = format.GetPixelSizeBit();

                var bufferSize = (pixelSize / 8) * w * h;

                var buffer = buffers?[result.Count] ?? MemoryBuffer.Create<byte>(bufferSize);

                buffer.Allocate(bufferSize);

                var item = new TextureData
                {
                    Width = w,
                    Height = h,
                    Format = format,
                    MipLevel = mipLevel,
                    Layer = face,
                    Data = buffer
                };

                GlUtils.GetPixelFormat(format, out var pixelFormat, out var pixelType);

                using var pData = buffer.MemoryLock();

                GlState.Current!.BindBuffer(BufferTargetARB.PixelPackBuffer, 0);

                gl.CheckError();

                gl.ReadPixels(0, 0, item.Width, item.Height, pixelFormat, pixelType, pData);

                gl.CheckError();

                result.Add(item);
            }

            src.Bind();

            _texReadFb ??= GlTempAllocator.FrameBuffer(gl, "TEX_READ");

            _texReadFb.BindRead(ReadBufferMode.ColorAttachment0);

            endMipLevel ??= src.MaxLevel;

            for (var mipLevel = startMipLevel; mipLevel <= endMipLevel; mipLevel++)
            {
                if (src.Target == TextureTarget.TextureCubeMap)
                {
                    for (var face = 0; face < 6; face++)
                        ReadTarget(TextureTarget.TextureCubeMapPositiveX + face, mipLevel, (uint)face);
                }
                else if (src.Target == TextureTarget.Texture2DArray)
                {
                    for (uint i = 0; i < src.Depth; i++)
                        ReadTarget(src.Target, mipLevel, 0, i);
                }
                else
                {
                    ReadTarget(src.Target, mipLevel);
                }
            }

            _texReadFb.Unbind();

            src.Unbind();

            return result;
        }
    }
}
