namespace XrEngine
{
    public interface IDrawGizmos
    {
        void DrawGizmos(Canvas3D canvas);

        bool IsEnabled { get; }
    }
}
