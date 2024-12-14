using Silk.NET.OpenXR;
using System.Numerics;
using System.Runtime.CompilerServices;
using XrMath;

namespace OpenXr.Framework
{
    public static class Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToVector3(this Vector3f value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }

        public static Posef ToPoseF(this Pose3 pose)
        {
            return new Posef
            {
                Orientation = pose.Orientation.ToQuaternionf(),
                Position = pose.Position.ToVector3f()
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3f ToVector3f(this Vector3 vector)
        {
            return new Vector3f(vector.X, vector.Y, vector.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternionf ToQuaternionf(this Quaternion quat)
        {
            return new Quaternionf(quat.X, quat.Y, quat.Z, quat.W);
        }

        public static unsafe Pose3 ToPose3(this Posef pose)
        {
            return new Pose3
            {
                Orientation = new Quaternion(pose.Orientation.X, pose.Orientation.Y, pose.Orientation.Z, pose.Orientation.W),
                Position = new Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z)
            };
        }

        public static void AddProjection(this XrLayerManager manager, RenderViewDelegate renderView)
        {
            manager.List.Add(new XrProjectionLayer(renderView));
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
