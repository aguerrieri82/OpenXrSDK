#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public class GlImageProc
    {
        readonly Dictionary<string, GlSimpleProgram> _programs = [];
        uint _emptyVertexArray;
        GlTextureFrameBuffer? _frameBuffer;

        protected GlSimpleProgram LoadProgram(GL gl, string fragmentSource, params string[] features)
        {
            if (!_programs.TryGetValue(fragmentSource, out var program))
            {
                program = new GlSimpleProgram(gl, "fullscreen.vert", fragmentSource, str => Embedded.GetString<Material>(str));
                foreach (var feature in features)
                    program.AddFeature(feature);
                program.Build();
                _programs[fragmentSource] = program;
            }
            program.Use();
            return program;
        }

        protected void DrawQuad(GL gl)
        {
            if (_emptyVertexArray == 0)
                _emptyVertexArray = gl.GenVertexArray();

            GlState.Current!.BindVertexArray(_emptyVertexArray);
            gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
        }

        protected void PrepareFrameBuffer(GL gl, GlTexture? color = null, IGlRenderAttachment? depth = null)
        {
            _frameBuffer ??= new GlTextureFrameBuffer(gl);
            _frameBuffer.Configure(color, depth, 1);
            _frameBuffer.Bind();
        }

        public void CopyDepth(IGlFrameBuffer src, GlTexture dst)
        {
            LoadProgram(src.GL, "copy_red.frag");

            GlState.Current!.LoadTexture((GlTexture)src.Depth!, 0);

            GlState.Current!.SetWriteDepth(false);
            GlState.Current!.SetUseDepth(false);
            GlState.Current!.SetWriteColor(true);

            PrepareFrameBuffer(src.GL, dst);
            DrawQuad(src.GL);
            src.Bind();
        }


        public static readonly GlImageProc Instance = new GlImageProc();
    }
}
