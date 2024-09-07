using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class PlaneGrid : LineMesh, IGeneratedContent
    {
        public PlaneGrid()
        {
        }

        public PlaneGrid(float size, float subs, float axisSize)
        {
            Size = size;
            Subs = subs;
            AxisSize = axisSize;
            ColorA = new Color(0.7f, 0.7f, 0.7f, 1);
            ColorB = new Color(0.1f, 0.1f, 0.1f, 1);
            Build();
        }

        public void Build()
        {
            var list = new List<LineData>();

            float subSize = Size / Subs;

            void AddLine(Vector3 from, Vector3 to, Color color, float size = 1f)
            {
                list.Add(new LineData
                {
                    Color = color,
                    Pos = from,
                    Size = size,
                });

                list.Add(new LineData
                {
                    Color = color,
                    Pos = to,
                    Size = size,
                });
            }

            float halfSize = Size / 2f;

            for (int z = 0; z < Subs; z++)
            {
                var zVal = -halfSize + z * subSize;

                AddLine(new Vector3(-halfSize, 0, zVal),
                        new Vector3(halfSize, 0, zVal),
                        zVal == 0 ? ColorB : ColorA
                       );

            }
            for (int x = 0; x < Subs; x++)
            {
                var xVal = -halfSize + x * subSize;

                AddLine(new Vector3(xVal, 0, -halfSize),
                        new Vector3(xVal, 0, halfSize),
                        xVal == 0 ? ColorB : ColorA
                       );
            }

            if (AxisSize > 0)
            {
                float yTop = 0.001f;

                AddLine(new Vector3(0, yTop, 0),
                       new Vector3(AxisSize, yTop, 0),
                       new Color(1, 0, 0, 1)
                );
                AddLine(new Vector3(0, yTop, 0),
                       new Vector3(0, AxisSize, 0),
                       new Color(0, 1, 0, 1)
                );
                AddLine(new Vector3(0, yTop, 0),
                     new Vector3(0, yTop, AxisSize),
                     new Color(0, 0, 1, 1)
                );
            }

            Vertices = list.ToArray();

            Version++;
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            container.ReadObject<PlaneGrid>(this);
            Build();
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.WriteObject<PlaneGrid>(this);
        }

        protected override void OnChanged(ObjectChange change)
        {
            if (change.Properties != null)
                Build();
            base.OnChanged(change);
        }

        public Color ColorA { get; set; }

        public Color ColorB { get; set; }

        public float Size { get; set; }

        public float Subs { get; set; }

        public float AxisSize { get; set; }
    }
}
