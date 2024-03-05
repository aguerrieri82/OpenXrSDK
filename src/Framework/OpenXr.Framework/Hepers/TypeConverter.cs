using Silk.NET.OpenXR;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace OpenXr.Framework
{
    public static unsafe class TypeConverter
    {
        [Obsolete("AndroidCrash")]
        public static unsafe XrPose _ToXrPose(this Posef pose)
        {
            return *(XrPose*)&pose;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToVector3(this Vector3f value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }


        public static Posef ToPoseF(this XrPose pose)
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

        public static unsafe XrPose ToXrPose(this Posef pose)
        {
            return new XrPose
            {
                Orientation = new Quaternion(pose.Orientation.X, pose.Orientation.Y, pose.Orientation.Z, pose.Orientation.W),
                Position = new Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z)
            };
        }

        public static unsafe XrSpaceLocation ToXrLocation(this SpaceLocation value)
        {
            return new XrSpaceLocation
            {
                Pose = value.Pose.ToXrPose(),
                Flags = value.LocationFlags
            };
        }

        public static Converter<TIn> Convert<TIn>(this ref TIn value) where TIn : unmanaged
        {
            return new Converter<TIn>(ref value);
        }

        public static ArrayConverter<TIn> Convert<TIn>(this TIn[] value) where TIn : unmanaged
        {
            return new ArrayConverter<TIn>(value);
        }
    }

    public unsafe ref struct Converter<TIn> where TIn : unmanaged
    {
        readonly ref TIn _value;

        public Converter(ref TIn value)
        {
            _value = ref value;
        }

        public TOut To<TOut>()
        {
            if (sizeof(TIn) < sizeof(TOut))
                throw new ArgumentException();

            fixed (TIn* pValue = &_value)
                return *(TOut*)pValue;
        }
    }

    public unsafe ref struct ArrayConverter<TIn> where TIn : unmanaged
    {
        readonly TIn[] _value;

        public ArrayConverter(TIn[] value)
        {
            _value = value;
        }

        public TOut[] To<TOut>()
        {
            if (sizeof(TIn) != sizeof(TOut))
                throw new ArgumentException();

            fixed (TIn* pValue = _value)
                return new Span<TOut>((TOut*)pValue, _value.Length).ToArray();
        }
    }
}
