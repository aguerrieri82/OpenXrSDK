using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.OpenGL
{
    public abstract class GlBaseRenderPass : IGlRenderPass
    {
        protected readonly OpenGLRender _renderer;
        protected bool _isInit;

        public GlBaseRenderPass(OpenGLRender renderer)
        {
            _renderer = renderer;
            IsEnabled = true;
        }

        public virtual void Initialize()
        {
        }   

        public void RenderContent(GlobalContent content)
        {
            if (!IsEnabled)
                return;

            if (!_isInit)
            {
                Initialize();
                _isInit = true; 
            }

            RenderContentWork(content); 
        }

        protected abstract void RenderContentWork(GlobalContent content);

        public bool IsEnabled { get; set; }
    }
}
