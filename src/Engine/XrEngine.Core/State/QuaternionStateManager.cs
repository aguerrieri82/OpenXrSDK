using System.Numerics;

namespace XrEngine
{
    public class QuaternionStateManager : ITypeStateManager<Quaternion>
    {
        QuaternionStateManager() { }

        public Quaternion Read(string key, Quaternion curObj, Type objType, IStateContainer container)
        {
            var parts = container.Read<float[]>(key);
            return new Quaternion(parts[0], parts[1], parts[2], parts[3]);
        }

        public void Write(string key, Quaternion obj, IStateContainer container)
        {
            container.Write(key, new float[] { obj.X, obj.Y, obj.Z, obj.W });
        }

        public static readonly QuaternionStateManager Instance = new();
    }
}
