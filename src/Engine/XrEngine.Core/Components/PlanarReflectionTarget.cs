namespace XrEngine
{

    public interface IPlanarReflectionTarget : IComponent
    {
        Func<PlanarReflection, bool>? IncludeReflection { get; }
    }

    public class PlanarReflectionTarget<T> : BaseComponent<T>, IPlanarReflectionTarget
        where T : Object3D, IVertexSource
    {

        public static PlanarReflectionTarget<T> ExcludeAll() => new()
        {
            IncludeReflection = _ => false
        };

        public Func<PlanarReflection, bool>? IncludeReflection { get; set; }

    }
}
