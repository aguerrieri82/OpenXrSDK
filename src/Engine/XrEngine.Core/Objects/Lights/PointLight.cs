using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class PointLight : Light
    {
        Vector3 _lastWorldPos;

        public PointLight()
        {
            Specular = Color.White;
            Range = 10;
        }

        protected internal override void InvalidateWorld()
        {
            base.InvalidateWorld();

            if (!_lastWorldPos.IsSimilar(WorldPosition, 0.001f))
            {
                _lastWorldPos = WorldPosition;
                Version++;
            }
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

        [Range(0, 100, 0.5f)]
        public float Range { get; set; }

        public Color Specular { get; set; }
    }
}
