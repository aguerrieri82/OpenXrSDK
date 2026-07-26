using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace XrEngine.OpenGL
{
    public struct RayPoniterTarget(IRayPointerProvider provider)
    {
        public IRayPointerProvider Provider = provider;

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is RayPoniterTarget other && Equals(Provider, other.Provider);
        }

        public override int GetHashCode()
        {
            return Provider.GetHashCode();
        }
    }

    public class GlRayColliderPassGroup : GlBaseRenderPassGroup<GlRayColliderPass, RayPoniterTarget>
    {
        readonly Dictionary<RayPoniterTarget, GlRayColliderPass> _passes = [];

        public GlRayColliderPassGroup(OpenGLRender renderer)
            : base(renderer)
        {
        }

        protected override GlRayColliderPass ConfigurePass(RayPoniterTarget options)
        {
            if (!_passes.TryGetValue(options, out var pass))
            {
                pass = new GlRayColliderPass(_renderer);
                pass.SetOptions(options);
                _passes[options] = pass;
            }

            return pass;
        }

        protected override IEnumerable<RayPoniterTarget> GetPasses(GlUpdateContext ctx)
        {
            var layer = ctx.Scene!.EnsureLayer<ComponentLayer<IRayPointerProvider>>();

            foreach (var content in layer.Content)
            {
                if (!content.IsVisible)
                    continue;

                foreach (var provider in content.Components<IRayPointerProvider>())
                {
                    if (!provider.IsEnabled)
                        continue;
                    yield return new RayPoniterTarget(provider);
                }
            }
        }
    }
}
