using System;
using System.Collections.Generic;
using System.Numerics;
using AvatarApi = global::Oculus.Avatar2.CAPI;

namespace XrEngine.OpenXr.Oculus
{

    public class AvatarMeshData : BaseComponent<TriangleMesh>
    {
        public Vector4[] VertexColors = [];
        public Vector4[] OrmtColors = [];
        public Vector2[] Uv2 = [];
        public string[] MorphTargetNames = [];
        public List<OculusAvatarMaterialProperty> MaterialExtensions = [];
        public Dictionary<AvatarApi.ovrAvatar2Id, Texture2D> ExtensionTextures = [];

        public AvatarMeshData Copy()
        {
            return new AvatarMeshData
            {
                VertexColors = VertexColors,
                OrmtColors = OrmtColors,
                Uv2 = Uv2,
                MorphTargetNames = MorphTargetNames,
                MaterialExtensions = MaterialExtensions,
                ExtensionTextures = ExtensionTextures
            };
        }
    }

    public class OculusAvatarMaterialProperty
    {
        public string Extension = "";
        public string Name = "";
        public AvatarApi.ovrAvatar2MaterialExtensionEntryType Type;
        public byte[] Data = [];
    }
}
