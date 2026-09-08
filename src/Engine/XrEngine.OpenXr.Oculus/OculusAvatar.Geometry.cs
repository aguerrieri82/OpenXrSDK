using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using XrEngine.Components;
using XrMath;
using AvatarApi = global::Oculus.Avatar2.CAPI;

namespace XrEngine.OpenXr.Oculus
{
    public partial class OculusAvatar
    {
        private sealed class PrimitiveData
        {
            public AvatarApi.ovrAvatar2Primitive Native;
            public string Name = "";
            public Geometry3D Geometry = null!;
            public AvatarApi.ovrAvatar2JointInfo[] Joints = [];
            public AvatarApi.ovrAvatar2MaterialTexture[] Textures = [];
            public OculusAvatarMeshData Details = null!;
        }

        private delegate AvatarApi.ovrAvatar2Result ReadVertexBuffer(
            AvatarApi.ovrAvatar2VertexBufferId id, IntPtr buffer, uint bytes, uint stride);

        private static unsafe T[] ReadBuffer<T>(
            AvatarApi.ovrAvatar2VertexBufferId id, uint count, ReadVertexBuffer read, bool optional = false)
            where T : unmanaged
        {
            var data = new T[count];
            var stride = (uint)sizeof(T);
            var byteCount = checked(count * stride);

            fixed (T* pointer = data)
            {
                var result = read(id, (IntPtr)pointer, byteCount, stride);

                if (optional && result == AvatarApi.ovrAvatar2Result.DataNotAvailable)
                    return [];

                Check(result);
            }

            return data;
        }

        private static unsafe PrimitiveData ReadPrimitive(AvatarApi.ovrAvatar2Primitive primitive)
        {
            var id = primitive.vertexBufferId;
            Check(AvatarApi.ovrAvatar2VertexBuffer_GetVertexCount(id, out var count));
            var positions = ReadBuffer<Vector3>(id, count, AvatarApi.ovrAvatar2VertexBuffer_GetPositions);
            var normals = ReadBuffer<Vector3>(id, count, AvatarApi.ovrAvatar2VertexBuffer_GetNormals, true);
            var tangents = ReadBuffer<Vector4>(id, count, AvatarApi.ovrAvatar2VertexBuffer_GetTangents, true);
            var uv = ReadBuffer<Vector2>(id, count, AvatarApi.ovrAvatar2VertexBuffer_GetTexCoord0, true);
            var uv1 = ReadBuffer<Vector2>(id, count, AvatarApi.ovrAvatar2VertexBuffer_GetTexCoord1, true);
            var vertices = new VertexData[count];

            for (int i = 0; i < count; i++)
            {
                vertices[i].Pos = positions[i];
                if (normals.Length > 0)
                    vertices[i].Normal = normals[i];
                if (tangents.Length > 0)
                    vertices[i].Tangent = tangents[i];
                if (uv.Length > 0)
                    vertices[i].UV = uv[i];
                if (uv1.Length > 0)
                    vertices[i].UV1 = uv1[i];
            }

            var indices = new ushort[primitive.indexCount];

            fixed (ushort* pointer = indices)
            {
                var byteCount = (uint)indices.Length * sizeof(ushort);
                var result = AvatarApi.ovrAvatar2Primitive_GetIndexData(primitive.id, pointer, byteCount);
                Check(result);
            }

            var geometry = new Geometry3D
            {
                Vertices = vertices,
                Indices = Array.ConvertAll(indices, value => (uint)value),
                ActiveComponents = VertexComponent.Position
            };

            if (normals.Length > 0)
                geometry.ActiveComponents |= VertexComponent.Normal;
            if (tangents.Length > 0)
                geometry.ActiveComponents |= VertexComponent.Tangent;
            if (uv.Length > 0)
                geometry.ActiveComponents |= VertexComponent.UV0;
            if (uv1.Length > 0)
                geometry.ActiveComponents |= VertexComponent.UV1;

            var details = new OculusAvatarMeshData
            {
                VertexColors = ReadBuffer<Vector4>(id, count, AvatarApi.ovrAvatar2VertexBuffer_GetColors, true),
                OrmtColors = ReadBuffer<Vector4>(id, count, AvatarApi.ovrAvatar2VertexBuffer_GetColorsORMT, true),
                Uv2 = ReadBuffer<Vector2>(id, count, AvatarApi.ovrAvatar2VertexBuffer_GetTexCoord2, true),
                MaterialExtensions = ReadMaterialExtensions(primitive.id)
            };

            var data = new PrimitiveData
            {
                Native = primitive,
                Name = ReadName(primitive.id),
                Geometry = geometry,
                Joints = new AvatarApi.ovrAvatar2JointInfo[primitive.jointCount],
                Textures = new AvatarApi.ovrAvatar2MaterialTexture[primitive.textureCount],
                Details = details
            };

            for (uint i = 0; i < data.Textures.Length; i++)
                Check(AvatarApi.ovrAvatar2Primitive_GetMaterialTextureByIndex(primitive.id, i, out data.Textures[i]));

            if (primitive.jointCount > 0)
            {
                var jointIndices = ReadBuffer<AvatarApi.ovrAvatar2Vector4us>(id, count, AvatarApi.ovrAvatar2VertexBuffer_GetJointIndices);
                var weights = ReadBuffer<Vector4>(id, count, AvatarApi.ovrAvatar2VertexBuffer_GetJointWeights);
                var skin = new SkinData[count];

                for (int i = 0; i < count; i++)
                {
                    var joint = jointIndices[i];
                    skin[i].JointIndices = new Vector4I(joint.x, joint.y, joint.z, joint.w);
                    skin[i].JointWeights = weights[i];
                }

                var skinned = geometry.AddComponent(new SkinnedGeometry());

                skinned.Skin = skin;
                geometry.ActiveComponents |= VertexComponent.Skin;

                fixed (AvatarApi.ovrAvatar2JointInfo* pointer = data.Joints)
                {
                    var byteCount = primitive.jointCount * (uint)sizeof(AvatarApi.ovrAvatar2JointInfo);
                    var result = AvatarApi.ovrAvatar2Primitive_GetJointInfo(primitive.id, pointer, byteCount);
                    Check(result);
                }
            }

            ReadMorphTargets(data, count);
            geometry.UpdateBounds();
            return data;
        }

        private unsafe delegate AvatarApi.ovrAvatar2Result ReadMorphAttribute(
            AvatarApi.ovrAvatar2MorphTargetBufferId id,
            uint targetIndex,
            AvatarApi.ovrAvatar2Vector3f* buffer,
            uint bytes,
            uint stride);

        private static unsafe Vector3[] ReadMorphValues(
            AvatarApi.ovrAvatar2MorphTargetBufferId id,
            uint targetIndex,
            uint vertexCount,
            ReadMorphAttribute read,
            bool optional = false)
        {
            var values = new Vector3[vertexCount];
            var stride = (uint)sizeof(Vector3);
            var byteCount = vertexCount * stride;
            AvatarApi.ovrAvatar2Result result;

            fixed (Vector3* pointer = values)
            {
                var buffer = (AvatarApi.ovrAvatar2Vector3f*)pointer;
                result = read(id, targetIndex, buffer, byteCount, stride);
            }

            if (optional && result == AvatarApi.ovrAvatar2Result.DataNotAvailable)
                return [];

            Check(result);
            return values;
        }

        private static unsafe void ReadMorphTargets(PrimitiveData primitive, uint vertexCount)
        {
            var id = primitive.Native.morphTargetBufferId;

            if (id == AvatarApi.ovrAvatar2MorphTargetBufferId.Invalid)
                return;

            var result = AvatarApi.ovrAvatar2VertexBuffer_GetMorphTargetCount(id, out var count);

            if (result == AvatarApi.ovrAvatar2Result.DataNotAvailable || result == AvatarApi.ovrAvatar2Result.NotFound)
                return;

            Check(result);

            if (count == 0)
                return;

            var targets = new MorphTarget[count];
            primitive.Details.MorphTargetNames = new string[count];

            for (uint i = 0; i < count; i++)
            {
                var positions = ReadMorphValues(id, i, vertexCount,
                    AvatarApi.ovrAvatar2MorphTarget_GetVertexPositions);

                var normals = ReadMorphValues(id, i, vertexCount,
                    AvatarApi.ovrAvatar2MorphTarget_GetVertexNormals, optional: true);

                var tangents = ReadMorphValues(id, i, vertexCount,
                    AvatarApi.ovrAvatar2MorphTarget_GetVertexTangents, optional: true);

                var name = ReadMorphName(id, i);

                var components = new List<MorphComponent>
                {
                    new() 
                    {
                        Component = VertexComponent.MorphPosition,
                        Values = positions,
                    }
                };

                if (normals.Length > 0)
                {
                    components.Add(new MorphComponent
                    {
                        Component = VertexComponent.MorphNormal,
                        Values = normals,
                    });
                }

                if (tangents.Length > 0)
                {
                    components.Add(new MorphComponent
                    {
                        Component = VertexComponent.MorphTangent,
                        Values = tangents,
                    });
                }

                primitive.Details.MorphTargetNames[i] = name;

                targets[i].Components = components.ToArray();
                targets[i].Name = name;
            }

            primitive.Geometry.AddComponent(new MorphedGeometry { Targets = targets });
        }

        private static unsafe string ReadMorphName(AvatarApi.ovrAvatar2MorphTargetBufferId id, uint index)
        {
            var name = new byte[1024];

            fixed (byte* pointer = name)
            {
                var result = AvatarApi.ovrAvatar2Asset_GetMorphTargetName(id, index, pointer, (uint)name.Length);
                Check(result);
            }

            return DecodeName(name);
        }
        private unsafe Group3D BuildAvatar()
        {
            Check(AvatarApi.ovrAvatar2Entity_GetPose(_entity, out var pose, out _));
            Check(AvatarApi.ovrAvatar2Render_QueryRenderState(_entity, out var render));
            
            var group = new Group3D { Name = "Meta Avatar" };
            
            SetTransform(group, render.rootTransform);

            var rig = group.AddChild(new Group3D { Name = "Skeleton" });

            var joints = new Joint3D[pose.jointCount];
            var jointNodes = new Dictionary<AvatarApi.ovrAvatar2NodeId, Joint3D>();

            for (int i = 0; i < joints.Length; i++)
            {
                joints[i] = new Joint3D { Name = ReadNodeName(pose.nodeIds[i]), InverseBindMatrix = Matrix4x4.Identity };
                SetTransform(joints[i], pose.localTransforms[i]);
                jointNodes.Add(pose.nodeIds[i], joints[i]);
            }

            for (int i = 0; i < joints.Length; i++)
            {
                var parent = pose.parents[i];

                if (parent < 0)
                    rig.AddChild(joints[i]);
                else
                    joints[parent].AddChild(joints[i]);
            }

            for (uint i = 0; i < render.primitiveCount; i++)
            {
                Check(AvatarApi.ovrAvatar2Render_GetPrimitiveRenderStateByIndex(_entity, i, out var state));
                
                var primitive = _primitives[state.primitiveId];

                var material = CreateMaterial(primitive);

                var mesh = new TriangleMesh(primitive.Geometry, material)
                {
                    Name = primitive.Name
                };

                SetTransform(mesh, primitive.Joints.Length > 0 ? state.skinningOrigin : state.localTransform);
                
                group.AddChild(mesh);

                mesh.AddComponent(primitive.Details.Copy());

                if (primitive.Joints.Length > 0)
                {
                    var palette = new Joint3D[primitive.Joints.Length];
                    for (int j = 0; j < palette.Length; j++)
                    {
                        var info = primitive.Joints[j];
                        var node = state.pose.nodeIds[info.jointIndex];
                        var joint = jointNodes[node];

                        // Bind matrices belong to each mesh; identity children share the animated joint pose.
                        palette[j] = joint.AddChild(new Joint3D
                        {
                            Name = mesh.Name + "." + joint.Name,
                            InverseBindMatrix = ToMatrix(info.inverseBind),
                            EnableGizmos = false
                        });
                    }

                    mesh.AddComponent(new MeshSkin { Joints = palette, SkinId = Guid.NewGuid() });

                    material.UseSkin = true;
                }

                if (state.morphTargetCount > 0)
                {
                    var weights = new float[state.morphTargetCount];

                    fixed (float* pointer = weights)
                    {
                        var byteCount = (uint)weights.Length * sizeof(float);
                        var result = AvatarApi.ovrAvatar2Render_GetMorphTargetWeights(
                            _entity, state.id, (IntPtr)pointer, byteCount);
                        Check(result);
                    }

                    mesh.AddComponent(new MeshMorph { Weights = weights });

                    material.UseMorph = true;
                    material.Morph = MorphMode.NotEmptyTargets;
                }
            }

            return group;
        }

        private static void SetTransform(Object3D target, AvatarApi.ovrAvatar2Transform value)
        {
            target.Transform.Position = new Vector3(value.position.x, value.position.y, value.position.z);
            target.Transform.Orientation = new Quaternion(value.orientation.x, value.orientation.y, value.orientation.z, value.orientation.w);
            target.Transform.Scale = new Vector3(value.scale.x, value.scale.y, value.scale.z);
        }

        private static Matrix4x4 ToMatrix(AvatarApi.ovrAvatar2Matrix4f m)
        {
            // Native column vectors -> System.Numerics row vectors.
            return new Matrix4x4(
                m.m00, m.m10, m.m20, m.m30,
                m.m01, m.m11, m.m21, m.m31,
                m.m02, m.m12, m.m22, m.m32,
                m.m03, m.m13, m.m23, m.m33);
        }

        private static unsafe string ReadName(AvatarApi.ovrAvatar2Id id)
        {
            var bytes = new byte[1024];

            fixed (byte* pointer = bytes)
            {
                var result = AvatarApi.ovrAvatar2Asset_GetPrimitiveName(id, pointer, (uint)bytes.Length);
                Check(result);
            }

            return DecodeName(bytes);
        }

        private unsafe string ReadNodeName(AvatarApi.ovrAvatar2NodeId id)
        {
            var bytes = new byte[1024];

            fixed (byte* pointer = bytes)
            {
                var result = AvatarApi.ovrAvatar2Entity_GetNodeName(_entity, id, pointer, (uint)bytes.Length, out _);
                Check(result);
            }

            return DecodeName(bytes);
        }

        private static string DecodeName(byte[] bytes)
        {
            var length = Array.IndexOf(bytes, (byte)0);
            return Encoding.UTF8.GetString(bytes, 0, length < 0 ? bytes.Length : length);
        }
    }
}
