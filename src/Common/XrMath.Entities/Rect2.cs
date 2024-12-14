using System.Numerics;

namespace XrMath
{
    public struct Rect2
    {
        public Rect2() { }

        public Rect2(float x, float y, float width, float height)
        {
            X = x; Y = y; Width = width; Height = height;
        }

        public readonly IEnumerable<Vector2> Corners
        {
            get
            {
                yield return new Vector2(X, Y);
                yield return new Vector2(X + Width, Y);
                yield return new Vector2(X + Width, Y + Height);
                yield return new Vector2(X, Y + Height);
            }
        }

        public void Expand(Size2 amount)
        {
            Width += amount.Width;
            Height += amount.Height;
        }

        public float Top
        {
            get => Y;
            set
            {
                var delta = (value - Y);
                Y += delta;
                Height -= delta;
            }
        }

        public float Left
        {
            get => X;
            set
            {
                var delta = (value - X);
                X += delta;
                Width -= delta;
            }
        }

        public float Bottom
        {
            get => Y + Height;
            set => Height = (value - Y);
        }

        public float Right
        {
            get => X + Width;
            set => Width = (value - X);
        }

        public float X;

        public float Y;

        public float Width;

        public float Height;

        public Vector2 Position
        {
            get => new(X, Y);
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        public Size2 Size
        {
            get => new(Width, Height);
            set
            {
                Width = value.Width;
                Height = value.Height;
            }
        }


        public static Rect2 FromCenter(float width, float height)
        {
            return new Rect2(-width / 2, -height / 2, width, height);
        }   
    }
}
