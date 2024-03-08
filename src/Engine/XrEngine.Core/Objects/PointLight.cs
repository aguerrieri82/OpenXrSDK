namespace XrEngine
{
    public class PointLight : Light
    {
        public PointLight()
        {
            Specular = Color.White;
        }

        public float Range { get; set; }

        public Color Specular { get; set; }
    }
}
