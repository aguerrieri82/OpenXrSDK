using System.Numerics;

namespace XrEngine
{
    public interface IWorldLocatable
    {
        Vector3 WorldPosition { get; set; }

        Quaternion WorldOrientation { get; set; }
    }
}
