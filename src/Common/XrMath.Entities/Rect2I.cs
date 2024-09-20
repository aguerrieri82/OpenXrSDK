namespace XrMath
{
    public struct Rect2I
    {
        public Rect2I()
        {

        }

        public Rect2I(int x, int y, uint width, uint height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int X;

        public int Y;

        public uint Width;

        public uint Height;
    }
}
