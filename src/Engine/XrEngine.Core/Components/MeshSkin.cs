
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
                _host.InvalidateBounds();
            }

        }

        public Bounds3 GetWorldBounds()
        {
            var builder = new Bounds3Builder();

            if (_host.Geometry == null)
                throw new InvalidOperationException();

            var geo = (SkinnedGeometry3D)_host.Geometry;

            if (Joints != null && Joints.Length == geo.JointBounds.Length)
            {
                for (var i = 0; i < Joints.Length; i++)
                {
                    var points = geo.JointBounds[i].Points;

                    var matrix = Joints[i].InverseBindMatrix * Joints[i].WorldMatrix;

                    foreach (var p in points)
                        builder.Add(p.Transform(matrix));
                }
            }

            return builder.Result;
        }


        public Joint3D[]? Joints { get; set; }

        public Matrix4x4[] SkinMatrices => _skinMatrices;

        public long SkinMatricesVersion => _skinMatricesVersion;
    }
}
