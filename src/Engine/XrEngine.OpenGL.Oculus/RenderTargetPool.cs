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

        protected uint CreateDepth(GlTexture color, uint arraySize)
        {
            var depthTex = _gl.GenTexture();

            GlState.Current!.BindTexture(color.Target, depthTex);

            if (arraySize == 1)
            {
                _gl.TexStorage2D(
                   color.Target,
                   1,
                   SizedInternalFormat.Depth32fStencil8,
                   color.Width,
                   color.Height);
            }
            else
            {
                _gl.TexStorage3D(
                   color.Target,
                   1,
                   SizedInternalFormat.Depth32fStencil8,
                   color.Width,
                   color.Height,
                   arraySize);
            }


            GlState.Current!.BindTexture(color.Target, 0);

            return depthTex;
        }

        public IGlRenderTarget GetRenderTarget(uint colorTex, uint sampleCount)
        {
            if (!_targets.TryGetValue(colorTex, out var target))
            {
                var color = GlTexture.Attach(_gl, colorTex);   

                uint depthTex = 0;  

                if (_multiView)
                {
                    var multiView = new GlMultiViewRenderTarget(_gl);
                    depthTex = CreateDepth(color, 2);
                    multiView.FrameBuffer.Configure(colorTex, depthTex, sampleCount);
                    target = multiView;
                }
                else
                {
                    var singleView = new GlTextureRenderTarget(_gl);
                    if (sampleCount > 0)
                    {
                        var renderBuf = new GlRenderBuffer(_gl); 
                        var glColorTex = GlTexture.Attach(_gl, colorTex); 
                        renderBuf.Update(glColorTex.Width, glColorTex.Height, sampleCount, InternalFormat.Depth32fStencil8);
                        singleView.FrameBuffer.Configure(glColorTex, renderBuf, sampleCount);
                    }
                    else
                    {
                        depthTex = CreateDepth(color, 1);
                        singleView.FrameBuffer.Configure(colorTex, depthTex, sampleCount);
                    }
        
                    target = singleView;
                }
                
                _targets[colorTex] = target;
            }

            return target;
        }
    }
}
