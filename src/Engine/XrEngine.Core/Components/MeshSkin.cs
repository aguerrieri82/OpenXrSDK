
using System.Numerics;
using XrEngine.Helpers;
using XrEngine.Objects;
using XrMath;


namespace XrEngine
{
    public class MeshSkin : Behavior<TriangleMesh>, ISkinnedMesh
    {
        protected Matrix4x4[] _skinMatrices = [];
        protected long _skinMatricesVersion = 0;

        protected override void Update(RenderContext ctx)
        {
            if (!_host.IsVisible)
                return;

            if (Joints == null)
            {
                if (_skinMatrices != null && _skinMatrices.Length > 0)
                    _skinMatrices = [];
                return;
            }

            if (_skinMatrices == null || _skinMatrices.Length != Joints.Length)
                _skinMatrices = new Matrix4x4[Joints.Length];

            HashBuilder.Instance.Reset();
            for (var i = 0; i < _skinMatrices.Length; i++)
            {
                _skinMatrices[i] = Joints[i].InverseBindMatrix * Joints[i].WorldMatrix * _host.WorldMatrixInverse;

                HashBuilder.Instance.Add(_skinMatrices[i]);
            }

            var curVer = (long)HashBuilder.Instance.Value();
            if (curVer != _skinMatricesVersion)
            {
                _skinMatricesVersion = curVer;
                _host.InvalidateLocalBounds();
            }

        }

        public Bounds3 GetLocalBounds()
        {
            var builder = new Bounds3Builder();

            if (_host.Geometry == null)
                throw new InvalidOperationException();

            var geo = _host.Geometry.Component<SkinnedGeometry>();

            if (_skinMatrices != null && _skinMatrices.Length > 0)
            {
                foreach (var entry in geo.JointBounds)
                {
                    if (entry.Key >= _skinMatrices.Length)
                        continue;
                    
                    var points = entry.Value.Points;

                    foreach (var p in points)
                        builder.Add(p.Transform(_skinMatrices[entry.Key]));
                }
            }

            return builder.Result;
        }


        public Joint3D[]? Joints { get; set; }

        public Matrix4x4[] SkinMatrices => _skinMatrices;

        public long SkinMatricesVersion => _skinMatricesVersion;

        public Guid SkinId { get; set; }
    }
}
