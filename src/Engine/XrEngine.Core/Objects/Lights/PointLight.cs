using XrMath;

namespace XrEngine
{
    public class PointLight : Light
    {
        public PointLight()
        {
            Specular = Color.White;
            Range = 10;
        }

        public float Range { get; set; }

        public Color Specular { get; set; }
    }
}
