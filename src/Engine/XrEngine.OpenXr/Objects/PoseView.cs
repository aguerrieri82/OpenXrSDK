using System.Numerics;
using XrMath;

namespace XrEngine.OpenXr
{
    public class PoseView : Group3D
    {
        readonly TriangleMesh _rayX;
        readonly TriangleMesh _rayY;
        readonly TriangleMesh _rayZ;
        readonly TriangleMesh _box;
        readonly TriangleMesh[] _rays;

        public PoseView(Color color)
        {
            RayLength = 0.2f;
            RayTicknesses = 0.002f;
            BoxSize = 0.02f;

            _box = AddChild(new TriangleMesh(Cube3D.Default, new PbrV2Material
            {
                Color = color,
                Simplified = true
            }));


            TriangleMesh CreateRay(Vector3 axis)
            {
                var res = AddChild(new TriangleMesh(Cube3D.Default, new PbrV2Material
                {
                    //Color = new Color(axis.X, axis.Y, axis.Z),
                    Color = color,
                    Simplified = true
                })
                {
                    Forward = axis,
                });

                res.Transform.LocalPivot = new Vector3(0, 0, 0.5f);
                return res;
            }

            _rayZ = CreateRay(-Vector3.UnitZ);
            _rays = [_rayZ];

            /*
            _rayX = CreateRay(Vector3.UnitX);
            _rayY = CreateRay(Vector3.UnitY);
            _rayZ = CreateRay(Vector3.UnitZ);

            _rays = [_rayX, _rayY, _rayZ];
            */

            Update();
        }

        public PoseView(Pose3 pose, string name, Color color)
            : this(color)
        {
            Transform.Set(pose.ToMatrix());
            Name = name;
        }

        public void Update()
        {
            _box.Transform.SetScale(BoxSize);
            foreach (var ray in _rays)
                ray.Transform.SetScale(RayTicknesses, RayTicknesses, RayLength);
        }


        public float RayLength { get; set; }

        public float RayTicknesses { get; set; }

        public float BoxSize { get; set; }
    }
}
