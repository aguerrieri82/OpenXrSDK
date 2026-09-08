using System;
using System.Collections.Generic;
using System.Numerics;
using XrEngine.Components;
using XrMath;
using AvatarApi = global::Oculus.Avatar2.CAPI;

namespace XrEngine.OpenXr.Oculus
{
    internal class OculusAvatarBuilder
    {
        private readonly OculusAvatarResources _resources;

        public OculusAvatarBuilder(OculusAvatarResources resources)
        {
            _resources = resources;
        }

        public unsafe Avatar Build(AvatarApi.ovrAvatar2EntityId entity)
        {
            OculusAvatarResources.Check(AvatarApi.ovrAvatar2Entity_GetPose(entity, out var pose, out _));
            OculusAvatarResources.Check(AvatarApi.ovrAvatar2Render_QueryRenderState(entity, out var render));

            var group = new Avatar { Name = "Meta Avatar" };

            SetTransform(group, render.rootTransform);

            var rig = group.AddChild(new Group3D { Name = "Skeleton" });

            var joints = new Joint3D[pose.jointCount];
            var jointNodes = new Dictionary<AvatarApi.ovrAvatar2NodeId, Joint3D>();

            for (int i = 0; i < joints.Length; i++)
            {
                joints[i] = new Joint3D
                {
                    Name = ReadNodeName(entity, pose.nodeIds[i]),
                };

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
                OculusAvatarResources.Check(AvatarApi.ovrAvatar2Render_GetPrimitiveRenderStateByIndex(entity, i, out var state));

                var primitive = _resources.GetPrimitive(state.primitiveId);

                var material = _resources.CreateMaterial(primitive);

                var mesh = new TriangleMesh(primitive.Geometry, material)
                {
                    Name = primitive.Name
                };

                SetTransform(mesh, primitive.Joints.Length > 0 ? state.skinningOrigin : state.localTransform);

                group.AddChild(mesh);

                mesh.AddComponent(primitive.Details.Copy());

                var jointLen = primitive.Joints.Length;

                if (jointLen > 0)
                {
                    var skin = new MeshSkin 
                    { 
                        Joints = new Joint3D[jointLen], 
                        SkinId = Guid.NewGuid(),
                        InverseBindMatrices = new Matrix4x4[jointLen]
                    };

                    for (int j = 0; j < jointLen; j++)
                    {
                        var info = primitive.Joints[j];
                        var node = state.pose.nodeIds[info.jointIndex];
                        var joint = jointNodes[node];

                        skin.InverseBindMatrices[j] = ToMatrix(info.inverseBind);
                        skin.Joints[j] = joint;
                    }

                    mesh.AddComponent(skin);

                    material.UseSkin = true;
                }

                if (state.morphTargetCount > 0)
                {
                    var weights = new float[state.morphTargetCount];

                    fixed (float* pointer = weights)
                    {
                        var byteCount = (uint)weights.Length * sizeof(float);
                        var result = AvatarApi.ovrAvatar2Render_GetMorphTargetWeights(entity, state.id, (IntPtr)pointer, byteCount);

                        OculusAvatarResources.Check(result);
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

        private static unsafe string ReadNodeName(
            AvatarApi.ovrAvatar2EntityId entity,
            AvatarApi.ovrAvatar2NodeId id)
        {
            var bytes = new byte[1024];

            fixed (byte* pointer = bytes)
            {
                var result = AvatarApi.ovrAvatar2Entity_GetNodeName(entity, id, pointer, (uint)bytes.Length, out _);
                OculusAvatarResources.Check(result);
            }

            return OculusAvatarResources.DecodeName(bytes);
        }
    }
}