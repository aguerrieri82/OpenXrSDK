using System.Numerics;

namespace XrMath
{
    public struct Bounds2
    {
        public Vector2 Max;

        public Vector2 Min;

        public readonly IEnumerable<Vector2> Points
        {
            get
            {
                yield return new Vector2(Min.X, Min.Y);
                yield return new Vector2(Min.X, Max.Y);
                yield return new Vector2(Max.X, Max.Y);
                yield return new Vector2(Max.X, Min.Y);

            }
        }

        public readonly Vector2 Size => Max - Min;  

        public readonly Vector2 Center => (Max + Min) / 2;

        public static Bounds2 Zero { get; } = new();
    }
}
