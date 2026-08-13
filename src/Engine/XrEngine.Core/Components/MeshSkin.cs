using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using XrEngine.Helpers;


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

                _skinMatricesVersion = (long)HashBuilder.Instance.Value();
            }

     
        public Joint3D[]? Joints { get; set; }

        public Matrix4x4[] SkinMatrices => _skinMatrices;

        public long SkinMatricesVersion => _skinMatricesVersion;
    }
}
