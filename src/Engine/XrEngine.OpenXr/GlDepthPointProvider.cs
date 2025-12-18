#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using Common.Interop;
using System.Numerics;
using XrEngine.OpenGL;


namespace XrEngine.OpenXr
{
    internal class GlDepthPointProvider : IDisposable, IDepthPointProvider
    {
        readonly GlRenderPassTarget _target;
        readonly PerspectiveCamera _depthCamera;
        readonly GlBaseProgram _program;
        readonly GL _gl;
        uint _emptyVertexArray;

        public GlDepthPointProvider(GL gl)
        {
            _gl = gl;

            _target = new GlRenderPassTarget(gl);
            _target.DepthMode = TargetDepthMode.None;

            _depthCamera = new PerspectiveCamera();

            _program = new GlSimpleProgram(_gl, "fullscreen.vert", "depth_point.frag", s =>
            {
                if (s.EndsWith(".vert"))
                    return Embedded.GetString<Object3D>(s);
                return Embedded.GetString<GlDepthPointProvider>(s);
            });

            _program.Build();

            _emptyVertexArray = _gl.GenVertexArray();
        }

        public void Dispose()
        {
            _target.Dispose();
            _program.Dispose();
            _gl.DeleteVertexArray(_emptyVertexArray);
            _emptyVertexArray = 0;
        }

        public unsafe Vector3[]? ReadPoints(IEnvDepthProvider provider)
        {
            Texture2D? texture = provider.Acquire(_depthCamera);
            if (texture == null)
                return null;

            if (_depthCamera.Eyes == null || _depthCamera.Eyes.Length != 2)
                return null;

            OpenGLRender renderer = OpenGLRender.Current!;
            GlState glState = renderer.State;

            _target.Configure(texture.Width, texture.Height, TextureFormat.RgbFloat32);

            IGlRenderTarget? curTarget = renderer.RenderTarget;

            _target.RenderTarget!.Begin(_depthCamera);

            _program.Use();
            _program.LoadTexture(texture, 8);

            Matrix4x4.Invert(_depthCamera.Eyes[0].ViewProj, out Matrix4x4 mat0);
            Matrix4x4.Invert(_depthCamera.Eyes[1].ViewProj, out Matrix4x4 mat1);

            _program.SetUniform("uDepthViewProjInv[0]", mat0);
            _program.SetUniform("uDepthViewProjInv[1]", mat1);
            _program.SetUniform("uActiveEye", _depthCamera.ActiveEye);

            glState.SetUseDepth(false);
            glState.SetWriteDepth(false);
            glState.SetAlphaMode(AlphaMode.Opaque);

            glState.BindVertexArray(_emptyVertexArray);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 3);

            GlTextureFrameBuffer fb = (GlTextureFrameBuffer)((IGlFrameBufferProvider)_target.RenderTarget).FrameBuffer;

            IMemoryBuffer<Vector3> buffer = MemoryBuffer.Create<Vector3>(texture.Width * texture.Height);

            using MemoryLock<Vector3> data = buffer.MemoryLock();

            fb.SetReadBuffer(ReadBufferMode.ColorAttachment0);

            _gl.ReadPixels(0, 0, texture.Width, texture.Height, PixelFormat.Rgb, PixelType.Float, data.Data);

            _target.RenderTarget.End(false);

            return buffer.AsSpan().ToArray();
        }
    }
}
