using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Animation
{
    public static class EngineExtensions
    {
        public static void Animate(this Object3D self, string? animationName = null, bool selfOnly = false)
        {
            if (!self.Scene!.TryComponent<AnimationController>(out var controller))
                controller = self.Scene!.AddComponent<AnimationController>();

            var items = self.DescendantsOrSelfWithFeature<IAnimationsHost>();

            foreach (var item in items)
            {
                if (item.Object != self && selfOnly)
                    continue;

                foreach (var animation in item.Feature.Animations)
                {
                    if (!string.IsNullOrWhiteSpace(animationName) && animationName != animation.Name)
                        continue;
                    controller.Start(animation, item.Object);
                }
            }
        }
    }
}
