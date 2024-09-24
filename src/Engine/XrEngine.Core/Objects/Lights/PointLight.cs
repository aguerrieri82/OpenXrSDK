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



        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.WriteObject<PointLight>(this);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            container.ReadObject(this);
        }

        public float Range { get; set; }

        public Color Specular { get; set; }
    }
}
