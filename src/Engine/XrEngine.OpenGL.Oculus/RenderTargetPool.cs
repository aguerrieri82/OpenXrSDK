using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if GLES

using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL.Oculus
{
    public class FrameBufferPool
    {
        private GL _gl;
        private bool _multiView;
        private Dictionary<uint, IGlRenderTarget> _targets = [];

        public FrameBufferPool(GL gl, bool multiView)
        {
            _gl = gl;
            _multiView = multiView;
        }

        protected GlTexture CreateDepth(GlTexture color, uint arraySize)
        {
            var depthTex = new GlTexture(_gl);
            depthTex.MinFilter = TextureMinFilter.Nearest;
            depthTex.MagFilter = TextureMagFilter.Nearest;
            depthTex.MaxLevel = 0;

            if (arraySize == 1)
            {
                depthTex.Target = TextureTarget.Texture2D;
                depthTex.Update(color.Width, color.Height, 1, TextureFormat.Depth32Stencil8);
            }
            else
            {
                depthTex.Target = TextureTarget.Texture2DArray;
                depthTex.Update(color.Width, color.Height, arraySize, TextureFormat.Depth32Stencil8);
            }

            return depthTex;
        }

        public IGlRenderTarget GetRenderTarget(uint colorTex, uint sampleCount)
        {
            if (!_targets.TryGetValue(colorTex, out var target))
            {
                var glColor = GlTexture.Attach(_gl, colorTex);

                GlTexture glDepth;

                if (_multiView)
                {
                    var multiView = new GlMultiViewRenderTarget(_gl);
                    glDepth = CreateDepth(glColor, 2);
                    multiView.FrameBuffer.Configure(glColor, glDepth, sampleCount);
                    target = multiView;
                }
                else
                {
                    var singleView = new GlTextureRenderTarget(_gl);
                    if (sampleCount > 0)
                    {
                        var renderBuf = new GlRenderBuffer(_gl); 
                        renderBuf.Update(glColor.Width, glColor.Height, sampleCount, InternalFormat.Depth32fStencil8);
                        singleView.FrameBuffer.Configure(glColor, renderBuf, sampleCount);
                    }
                    else
                    {
                        glDepth = CreateDepth(glColor, 1);
                        singleView.FrameBuffer.Configure(glColor, glDepth, sampleCount);
                    }
        
                    target = singleView;
                }
                
                _targets[colorTex] = target;
            }

            return target;
        }
    }
}
