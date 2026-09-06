namespace XrEngine
{
    public interface IGeometryComponent : IComponent<Geometry3D>
    {
        void NotifyLoaded() { }

        void UpdateBounds() { }

    }
}
