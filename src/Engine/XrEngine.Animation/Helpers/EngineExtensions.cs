namespace XrEngine.Animation
{
    public static class EngineExtensions
    {
        public static AnimationBuilder<THost> Animate<THost>(this THost self) where THost : Object3D
        {
            return new AnimationBuilder<THost>(self);
        }


        public static IAnimationControl Animate<TOptions>(this Object3D self, string animationName, TOptions options)
        {
            var manager = self.Scene!.EnsureComponent<AnimationManager>();
            var host = self.Component<AnimationsHost>();
            var anim = host.Animations.First(a => a.Name == animationName);

            if (anim is not IComputedAnimation computedAnim)
                throw new InvalidOperationException();

            if (computedAnim.Compute is not IOptionsProvider<TOptions> computeOptions)
                throw new InvalidOperationException();

            computeOptions.Options = options;

            var control = manager.Create(anim, self);

            control.Play();
            
            return control;
        }

        public static IAnimationControl Animate(this Object3D self, string animationName)
        {
            var manager = self.Scene!.EnsureComponent<AnimationManager>();
            var host = self.Component<AnimationsHost>();
            var anim = host.Animations.First(a => a.Name == animationName);
            var control = manager.Create(anim, self);
            control.Play();
            return control;
        }

        public static void AnimateAll(this Object3D self, string? animationName = null)
        {
            var manager = self.Scene!.EnsureComponent<AnimationManager>();

            var items = self.DescendantsOrSelfWithFeature<IAnimationsHost>();

            foreach (var item in items)
            {
                foreach (var animation in item.Feature.Animations)
                {
                    if (!string.IsNullOrWhiteSpace(animationName) && animationName != animation.Name)
                        continue;

                    manager.Create(animation, item.Object).Play();
                }
            }
        }
    }
}
