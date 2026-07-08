#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.OpenGL
{
    public class GlSwapTexture : IDisposable
    {
        private GlTexture _activeTex;
        private GlTexture _backTex;

        private GlTexture _temp;
        private GlTexture _main;

        public GlSwapTexture(Texture main)
            : this(main.ToGlTexture())
        {

        }

        public GlSwapTexture(GlTexture main)
        {
            _main = main;

            _temp = new GlTexture(_main.GL)
            {
                MaxLevel = _main.MaxLevel,
                MinFilter = _main.MinFilter,
                MagFilter = _main.MagFilter 
            };

            _temp.Allocate(_main.Width, _main.Height, _main.Depth, _main.InternalFormat.GetTextureFormat());

            if (_main.MaxLevel > 0)
                _temp.GenerateMipmap();

            _activeTex = _main;
            _backTex = _temp;
        }

        public static GlSwapTexture Create(GL gl, uint width, uint height, uint depth, TextureFormat format)
        {
            var main = new GlTexture(gl);
            main.Allocate(width, height, depth, format);
            return new GlSwapTexture(main);
        }

        public void Update(uint width, uint height, uint depth, TextureFormat format)
        {
            _main.Allocate(width, height, depth, format);
            _temp.Allocate(width, height, depth, format);
        }

        public void Update(uint width, uint height)
        {
            Update(width, height, _main.Depth, _main.InternalFormat.GetTextureFormat());
        }

        public void Blur(int passes = 1, int mipLevel = 0)
        {
            for (var i = 0; i < passes; i++)
            {
                GlTextureFilter.Instance!.Blur(
                    (Texture2D)_activeTex.ToEngineTexture(), 
                    (Texture2D)_backTex.ToEngineTexture(), $"Blur_Swap_{_main.InternalFormat}_{mipLevel}", 3, mipLevel);

                Swap();
            }
        }

        public void CopyAndSwap()
        {
            _activeTex.CopyTo(_backTex);
            Swap();
        }

        public void Swap()
        {
            (_activeTex, _backTex) = (_backTex, _activeTex);
        }

        public void Dispose()
        {
            _temp.Dispose();
            _main.Dispose();
            GC.SuppressFinalize(this);
        }

        public GlTexture Active => _activeTex;
    }
}
