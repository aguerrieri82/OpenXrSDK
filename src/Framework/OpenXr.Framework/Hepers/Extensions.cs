using OpenXr.Framework.Layers;
using Silk.NET.OpenXR;
using System.Numerics;
using System.Runtime.CompilerServices;

using XrMath;

namespace OpenXr.Framework
{
    public static class Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToVector3(this in Vector3f value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToVector2(this in Vector2f value)
        {
            return new Vector2(value.X, value.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 ToVector4(this in Vector4f value)
        {
            return new Vector4(value.X, value.Y, value.Z, value.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Posef ToPoseF(this in Pose3 pose)
        {
            return new Posef
            {
                Orientation = pose.Orientation.ToQuaternionf(),
                Position = pose.Position.ToVector3f()
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3f ToVector3f(this in Vector3 vector)
        {
            return new Vector3f(vector.X, vector.Y, vector.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternionf ToQuaternionf(this in Quaternion quat)
        {
            return new Quaternionf(quat.X, quat.Y, quat.Z, quat.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pose3 ToPose3(this in Posef pose)
        {
            return new Pose3
            {
                Orientation = new Quaternion(pose.Orientation.X, pose.Orientation.Y, pose.Orientation.Z, pose.Orientation.W),
                Position = new Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z)
            };
        }

        public static XrQuadLayer AddQuod(this XrLayerManager manager, GetQuadDelegate getQuad, RenderGeometryLayerDelegate render, Size2I size, int priority = XrLayerPriority.BaseQuods)
        {
            var source = new XrTextureLayerSource(render, size);
            var layer = new XrQuadLayer(getQuad, source)
            {
                Priority = priority
            };

            manager.Add(layer);

            return layer;
        }

        public static XrQuadLayer[] AddStereoQuod(this XrLayerManager manager, GetQuadDelegate getQuad, RenderGeometryLayerDelegate render, Size2I size, int priority = XrLayerPriority.BaseQuods)
        {
            var swapchain = new XrSwapchain(XrApp.Current!, 2);

            var source0 = new XrTextureLayerSource(render, size);
            var source1 = new XrTextureLayerSource(render, size);

            source0.ConfigureStereo(swapchain, 0);
            source1.ConfigureStereo(swapchain, 1);

            var eye0 = new XrQuadLayer(getQuad, source0)
            {
                Priority = priority
            };

            var eye1 = new XrQuadLayer(getQuad, source1)
            {
                Priority = priority
            };

            manager.Add(eye0);
            manager.Add(eye1);

            return [eye0, eye1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static XrProjectionLayer AddProjection(this XrLayerManager manager, RenderViewDelegate renderView, bool useDepthSwapchain)
        {
            var layer = new XrProjectionLayer(renderView, useDepthSwapchain);
            manager.List.Add(layer);
            return layer;
        }

        public static void ScheduleCancel<T>(this TaskCompletionSource<T> completionSource, TimeSpan time)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(time);
                if (!completionSource.Task.IsCompleted)
                    completionSource.SetCanceled();
            });
        }
    }
}
