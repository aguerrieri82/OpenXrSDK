namespace XrEngine.Animation
{
    public static class EngineExtensions
    {
        public static AnimationBuilder<T> Animate<T>(this T self) where T : Object3D
        {
            return new AnimationBuilder<T>(self);
        }

        public static void Animate(this Object3D self, string? animationName = null, bool selfOnly = false)
        {
            if (!self.Scene!.TryComponent<AnimationManager>(out var controller))
                controller = self.Scene!.AddComponent<AnimationManager>();

            var items = self.DescendantsOrSelfWithFeature<IAnimationsHost>();

            foreach (var item in items)
            {
                if (item.Object != self && selfOnly)
                    continue;

                foreach (var animation in item.Feature.Animations)
                {
                    if (!string.IsNullOrWhiteSpace(animationName) && animationName != animation.Name)
                        continue;

                    controller.Create(animation, item.Object).Play();
                }
            }
        }
    }
}
