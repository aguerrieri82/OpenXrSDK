namespace OpenXr.Engine
{
    public class PointLight : Light
    {
        public PointLight()
        {
            Specular = Color.White;
        }

        public Color Specular { get; set; }
    }
}
