using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.OpenGL
{
    public struct ReflectionTarget
    {
        public ReflectionTarget(PlanarReflection planarReflection, int boundEye = -1)
        {
            PlanarReflection = planarReflection;
            BoundEye = boundEye;
        }

        public readonly PlanarReflection PlanarReflection;

        public readonly int BoundEye;
    }

    public class GlReflectionPass : GlBaseRenderPassGroup<IGlDynamicRenderPass<ReflectionTarget>, ReflectionTarget>
    {
        GlSimpleReflectionTargetPass? _simple;
        GlFullReflectionTargetPass? _full;

        public GlReflectionPass(OpenGLRender renderer)
            : base(renderer)
        {
            UseMultiviewTarget = true;
        }   

        protected override IGlDynamicRenderPass<ReflectionTarget> ConfigurePass(ReflectionTarget options)
        {
            if (options.PlanarReflection.Mode == PlanarReflectionMode.Full)
            {
                _full ??= new(_renderer, UseMultiviewTarget);
                _full.SetOptions(options);
                return _full;
            }

            _simple ??= new(_renderer, UseMultiviewTarget);
            _simple.SetOptions(options);
            return _simple;
        }

        protected override IEnumerable<ReflectionTarget> GetPasses(RenderContext ctx)
        {
            var layer = ctx.Scene!.EnsureLayer<HasReflectionLayer>();

            foreach (var content in layer.Content)
            {
                if (!content.IsVisible)
                    continue;
                
                var reflection = content.Component<PlanarReflection>();
                if (!reflection.IsEnabled)
                    continue;

                if (PlanarReflection.IsMultiView && !UseMultiviewTarget)
                {
                    yield return new ReflectionTarget(reflection, 0);
                    yield return new ReflectionTarget(reflection, 1);
                }
                else
                    yield return new ReflectionTarget(reflection);
            }
        }


        public bool UseMultiviewTarget { get; set; }
    }
}
