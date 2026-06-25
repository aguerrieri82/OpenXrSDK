namespace XrEngine
{
    internal readonly struct TriangleIndex3
    {
        public TriangleIndex3(uint a, uint b, uint c)
        {
            A = a;
            B = b;
            C = c;
        }

        public TriangleIndex3(int a, int b, int c)
            : this((uint)a, (uint)b, (uint)c)
        {
        }

        public readonly uint A;

        public readonly uint B;

        public readonly uint C;
    }
}