namespace XrMath
{
    public struct Rect2I
    {
        public Rect2I()
        {

        }

        public Rect2I(Size2I size)
        {
            Width = size.Width;
            Height = size.Height;
        }

        public Rect2I(int x, int y, uint width, uint height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public Size2I Size => new Size2I(Width, Height);

        public int Right => X + (int)Width;

        public int Bottom => Y + (int)Height;

        public int Left
        {
            readonly get => X;
            set
            {
                var right = Right;
                X = value;
                Width = (uint)Math.Max(0, right - X);
            }
        }

        public int Top
        {
            readonly get => Y;
            set
            {
                var bottom = Bottom;
                Y = value;
                Height = (uint)Math.Max(0, bottom - Y);
            }
        }

        public readonly bool IsEmpty => Width == 0 || Height == 0;

        public int X;

        public int Y;

        public uint Width;

        public uint Height;

        public static readonly Rect2I Empty = new();
    }
}
