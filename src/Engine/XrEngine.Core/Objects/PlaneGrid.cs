using System.Numerics;

namespace XrEngine
{
    public class PlaneGrid : LineMesh
    {
        public PlaneGrid()
        {
        }

        public PlaneGrid(float size, float subs, float axisSize)
        {
            Size = size;
            Subs = subs;
            AxisSize = axisSize;
            Create();
        }

        public void Create()
        {
            var list = new List<LineData>();

            float subSize = Size / Subs;

            var color1 = new Vector3(0.7f, 0.7f, 0.7f);

            var color2 = new Vector3(0.1f, 0.1f, 0.1f);

            void AddLine(Vector3 from, Vector3 to, Vector3 color, float size = 1f)
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
                        zVal == 0 ? color2 : color1
                       );

            }
            for (int x = 0; x < Subs; x++)
            {
                var xVal = -halfSize + x * subSize;

                AddLine(new Vector3(xVal, 0, -halfSize),
                        new Vector3(xVal, 0, halfSize),
                        xVal == 0 ? color2 : color1
                       );
            }

            if (AxisSize > 0)
            {
                float yTop = 0.001f;

                AddLine(new Vector3(0, yTop, 0),
                       new Vector3(AxisSize, yTop, 0),
                       new Vector3(1, 0, 0)
                );
                AddLine(new Vector3(0, yTop, 0),
                       new Vector3(0, AxisSize, 0),
                       new Vector3(0, 1, 0)
                );
                AddLine(new Vector3(0, yTop, 0),
                     new Vector3(0, yTop, AxisSize),
                     new Vector3(0, 0, 1)
                );
            }

            Vertices = list.ToArray();

            Version++;
        }

        protected override void SetStateWork(StateContext ctx, IStateContainer container)
        {
            base.SetStateWork(ctx, container);
            container.ReadObject<PlaneGrid>(this);
            Create();
        }

        public override void GetState(StateContext ctx, IStateContainer container)
        {
            base.GetState(ctx, container);  
            container.WriteObject<PlaneGrid>(this);
        }

        public float Size { get; set; }
            
        public float Subs { get; set; }

        public float AxisSize { get; set; }
    }
}
