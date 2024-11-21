using System.Numerics;


namespace XrMath
{
    public class DiscreteFunction : IFunction2
    {
        public Bounds1 RangeX => new Bounds1
        {
            Min = Points == null || Points.Length == 0 ? 0 : Points[0].X,
            Max = Points == null || Points.Length == 0 ? 0 : Points[^1].X
        };

        public Bounds2 Bounds()
        {
            if (Points.Length == 0)
                return Bounds2.Zero;

            var result = new Bounds2()
            {
                Min = Points[0],
                Max = Points[0]
            };

            foreach (var point in Points.Skip(1))
            {
                result.Max = Vector2.Max(result.Max, point);
                result.Min = Vector2.Min(result.Min, point);
            }

            return result;
        }

        float InterpolateY(int index, float targetX)
        {
            if (index == Points.Length - 1)
                return Points[index].Y;

            var p1 = Points[index];
            var p2 = Points[index + 1];

            var t = (targetX - p1.X) / (p2.X - p1.X);
            return p1.Y + (p2.Y - p1.Y) * t;
        }

        public int IndexOfClosestX(float targetX)
        {
            if (Points.Length == 0)
                return -1;

            int left = 0;
            int right = Points.Length - 1;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;

                if (Points[mid].X == targetX)
                {
                    return mid;
                }
                else if (Points[mid].X < targetX)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            if (right == -1)
                right = 0;

            return right;
        }

        public float Value(float targetX)
        {
            var index = IndexOfClosestX(targetX);
            if (index == -1)
                return float.NaN;
            return InterpolateY(index, targetX);
        }

        public void Reflect()
        {
            var bounds = Bounds();
            for (var i = 0; i < Points.Length; i++)
                Points[i].Y = bounds.Max.Y - Points[i].Y;
        }


        public Vector2[] Points = [];
    }
}
