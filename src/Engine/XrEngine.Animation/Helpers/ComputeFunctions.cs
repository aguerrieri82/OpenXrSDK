using System.Numerics;

namespace XrEngine.Animation
{
    public class SinOptions
    {
        public Vector3 Axis { get; set; }
        public float Amplitude { get; set; }
        public float Frequency { get; set; }
        public float Phase { get; set; }
        public Vector3 Offset { get; set; }
        public float? Duration { get; set; }
    }

    public class JumpOptions
    {
        public float BaseY { get; set; }

        [ValueType(ValueType.Direction)]
        public Vector3 Direction { get; set; }
        
        public float Intensity { get; set; }

        public float Gravity { get; set; }
    }

    public class JumpImpulseOptions
    {
        public float BaseY { get; set; }
        public Vector3 Impulse { get; set; }
        public float Gravity { get; set; }
    }


    public static class ComputeFunctions
    {
        public static IComputeFunction<Vector3, SinOptions> Sin(
            Vector3 axis,
            float amplitude = 1f,
            float frequency = 1f,
            float phase = 0f,
            Vector3 offset = default,
            float? duration = null)
        {
            var options = new SinOptions
            {
                Axis = Vector3.Normalize(axis),
                Amplitude = amplitude,
                Frequency = frequency,
                Phase = phase,
                Offset = offset,
                Duration = duration
            };

            return new DelegateComputeFunction<Vector3, SinOptions>(
                (t, o) => o.Offset + o.Axis * (o.Amplitude * MathF.Sin(MathF.Tau * o.Frequency * t + o.Phase)),
                o => o.Duration ?? 1f / MathF.Abs(o.Frequency),
                options);
        }


        public static IComputeFunction<Vector3, JumpOptions> Jump(
            float baseY,
            Vector3 direction,
            float intensity = 1f,
            float gravity = 9.81f)
        {
            var options = new JumpOptions
            {
                BaseY = baseY,
                Direction = Vector3.Normalize(direction),
                Intensity = intensity,
                Gravity = gravity
            };

            return new DelegateComputeFunction<Vector3, JumpOptions>(
                (t, o) =>
                {
                    var impulse = o.Direction * o.Intensity;
                    var halfGravity = o.Gravity * 0.5f;

                    return new Vector3(
                        impulse.X * t,
                        o.BaseY + impulse.Y * t - halfGravity * t * t,
                        impulse.Z * t);
                },
                o => 2f * (o.Direction.Y * o.Intensity) / o.Gravity,
                options);
        }


        public static IComputeFunction<Vector3, JumpImpulseOptions> JumpImpulse(
            float baseY,
            Vector3 impulse,
            float gravity = 9.81f)
        {
            var options = new JumpImpulseOptions
            {
                BaseY = baseY,
                Impulse = impulse,
                Gravity = gravity
            };

            return new DelegateComputeFunction<Vector3, JumpImpulseOptions>(
                (t, o) =>
                {
                    var halfGravity = o.Gravity * 0.5f;

                    return new Vector3(
                        o.Impulse.X * t,
                        o.BaseY + o.Impulse.Y * t - halfGravity * t * t,
                        o.Impulse.Z * t);
                },
                o => 2f * o.Impulse.Y / o.Gravity,
                options);
        }
    }
}