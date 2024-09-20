﻿using System;
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

        protected virtual void Initialize()
        {
        }

        protected virtual IEnumerable<GlLayer> SelectLayers()
        {
           return _renderer.Layers.Where(a => a.Type != GlLayerType.CastShadow);  
        }

        public virtual void Render()
        {
            if (!IsEnabled)
                return;
            
            if (!_isInit)
            {
                Initialize();
                _isInit = true;
            }

            if (!BeginRender())
                return;
            
            foreach (var layer in SelectLayers())
                RenderLayer(layer);

            EndRender();
        }

        protected virtual bool BeginRender()
        {
            return true;
        }

        protected virtual void EndRender()
        {

        }

        protected abstract void RenderLayer(GlLayer layer);

        public bool IsEnabled { get; set; }
    }
}
