using System.Numerics;

namespace XrEngine
{
    public class Vector3StateManager : ITypeStateManager<Vector3>
    {
        Vector3StateManager() { }

        public Vector3 Read(string key, Vector3 destObj, Type objType, IStateContainer container)
        {
            var parts = container.Read<float[]>(key);
            return new Vector3(parts[0], parts[1], parts[2]);
        }

        public void Write(string key, Vector3 obj, IStateContainer container)
        {
            container.Write(key, new float[] { obj.X, obj.Y, obj.Z });
        }


        public static readonly Vector3StateManager Instance = new();
    }
}
