using System.Numerics;

namespace XrEngine
{
    public unsafe class Matrix4x4StateManager : ITypeStateManager<Matrix4x4>
    {
        Matrix4x4StateManager() { }

        public Matrix4x4 Read(string key, Matrix4x4 curObj, Type objType, IStateContainer container)
        {
            var array = container.Read<float[]>(key);
            fixed (float* pArray = array)
                return *(Matrix4x4*)pArray;
        }

        public void Write(string key, Matrix4x4 obj, IStateContainer container)
        {
            var floats = new Span<float>(&obj, 16);
            container.Write(key, floats.ToArray());
        }


        public static readonly Matrix4x4StateManager Instance = new();
    }
}
