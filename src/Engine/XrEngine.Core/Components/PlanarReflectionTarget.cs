namespace XrEngine
{
    public class PlanarReflectionTarget : BaseComponent<TriangleMesh>
    {

        public static PlanarReflectionTarget ExcludeAll() => new PlanarReflectionTarget
        {
            IncludeReflection = _ => false
        };

        public Func<PlanarReflection, bool>? IncludeReflection { get; set; }

    }
}
