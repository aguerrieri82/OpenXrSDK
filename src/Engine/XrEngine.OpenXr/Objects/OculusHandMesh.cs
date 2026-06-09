using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using System.Numerics;
using XrEngine.Objects;
using XrMath;

namespace XrEngine.OpenXr
{
    public class OculusHandMesh : TriangleMesh, ISkinnedMesh
    {
        readonly XrHandMesh _mesh;
        protected Matrix4x4[] _invBindMatrices;
        protected Matrix4x4[] _skinMatrices; 
        protected long _skinVersion;

        public OculusHandMesh(XrHandMesh mesh)
        {
            _mesh = mesh;

            Flags |= EngineObjectFlags.NoFrustumCulling;

            Materials.Add(new PbrV2Material
            {
                Color = "#ff0000",
                HasSkin = true
            });

            _skinMatrices = new Matrix4x4[_mesh.Joints!.Length];
            _invBindMatrices = new Matrix4x4[_mesh.Joints!.Length];

            CreateGeometry();
            BuildInverseBind();
        }

        void BuildInverseBind()
        {
            for (int i = 0; i < _mesh.Joints!.Length; i++)
            {
                var bind = _mesh.Joints[i].BindPose.ToPose3().ToMatrix();
                Matrix4x4.Invert(bind, out _invBindMatrices[i]);
            }
        }

        public void Update(HandJointLocationEXT[] joints)
        {
            for (int i = 0; i < _skinMatrices.Length; i++)
            {
                var current = joints[i].Pose.ToPose3().ToMatrix();

                _skinMatrices[i] = _invBindMatrices[i] * current;
            }
            _skinVersion++;
        }

        public void  CreateGeometry()
        {
            var geometry = new SkinnedGeometry3D
            {
                Vertices = new VertexData[_mesh.Vertices!.Length],
                Skin = new SkinData[_mesh.Vertices!.Length],
                Indices = _mesh.Indices!,
                ActiveComponents =
                    VertexComponent.Position |
                    VertexComponent.Normal |
                    VertexComponent.UV0
            };

            for (int i = 0; i < _mesh.Vertices.Length; i++)
            {
                var ix = _mesh.Vertices[i].BlendIndex;

                geometry.Vertices[i].Pos = _mesh.Vertices[i].Pos.ToVector3();
                geometry.Vertices[i].Normal = _mesh.Vertices[i].Normal.ToVector3();
                geometry.Vertices[i].UV = _mesh.Vertices[i].UV.ToVector2();
                geometry.Skin[i].JointIndices = new Vector4I(ix.X, ix.Y, ix.Z, ix.W);
                geometry.Skin[i].JointWeights = _mesh.Vertices[i].BlendWeight.ToVector4();
            }

            Geometry = geometry;    
        }

        public Matrix4x4[] SkinMatrices => _skinMatrices;

        SkinData[] ISkinnedMesh.Skin => ((SkinnedGeometry3D)_geometry!).Skin;

        public long SkinVersion => 1;

        public long SkinMatricesVersion => _skinVersion;
    }
}
