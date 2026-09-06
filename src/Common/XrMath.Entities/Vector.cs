namespace XrMath
{
    public struct Vector3<T> where T : unmanaged
    {
        public Vector3(T x, T y, T z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public T X;
        public T Y;
        public T Z;
    }

    public struct Vector4<T> where T : unmanaged
    {
        public Vector4(T x, T y, T z, T w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public T X;
        public T Y;
        public T Z;
        public T W;
    }

    public struct Vector2<T> where T : unmanaged
    {
        public Vector2(T x, T y)
        {
            X = x;
            Y = y;
        }

        public T X;
        public T Y;
    }

}
