namespace XrEngine
{
    public class PlanarReflectionTarget : BaseComponent<TriangleMesh>
    {

        public Func<PlanarReflection, bool>? IncludeReflection { get; set; }


        public static PlanarReflectionTarget ExcludeAll() => new PlanarReflectionTarget
        {
            IncludeReflection = _ => false
        };
    }
}
