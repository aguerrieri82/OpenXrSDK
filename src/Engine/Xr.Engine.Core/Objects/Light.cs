namespace Xr.Engine
{
    public abstract class Light : Object3D
    {
        public Light()
        {
            Color = Color.White;
            Intensity = 1f;
        }

        public Color Color { get; set; }

        public float Intensity { get; set; }
    }
}
