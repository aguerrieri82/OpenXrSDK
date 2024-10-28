using System.Numerics;

namespace XrEngine
{

    public class OrtoCamera : Camera
    {
        public OrtoCamera()
        {
        }

        public void SetViewArea(float left, float right, float bottom, float top)
        {
            Projection = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, Near, Far);
        }
    }
}
