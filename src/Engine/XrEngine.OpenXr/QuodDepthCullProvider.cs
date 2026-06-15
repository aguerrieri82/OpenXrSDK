using OpenXr.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using XrEngine.OpenGL;
using static XrEngine.QuixelMaterialReader;

namespace XrEngine.OpenXr
{
    public class QuodDepthCullProvider : IQuodDepthCull
    {
        readonly Dictionary<IQuodTexture, GlQuodCullPass> _passes = [];

        public void Cull(IQuodTexture texture)
        {
            var renderer = OpenGLRender.Current!;

            if (!_passes.TryGetValue(texture, out var pass))
            {
                var options = XrApp.Current!.RenderOptions;

                var isMultiView = options.RenderMode == XrRenderMode.MultiView;
                var sampleCount = XrPlatform.IsAndroid ? 1 : options.SampleCount;
                pass = new GlQuodCullPass(renderer, texture, isMultiView, sampleCount);
                _passes[texture] = pass;
            }

            renderer.PushGroup("GlQuodCullPass");

            pass.Render(new RenderContext
            {
                Camera = ((Object3D)texture).Scene!.ActiveCamera
            });

            renderer.PopGroup();
        }
    }
}
