using System.Numerics;

namespace XrEngine.Animation
{
    public static class ComputeFunctions
    {
        public static ComputeFunction<Vector3> Sin(
            Vector3 axis,
            float amplitude = 1f,
            float frequency = 1f,
            float phase = 0f,
            Vector3 offset = default,
            float? duration = null)
        {
            axis = Vector3.Normalize(axis);

            return new ComputeFunction<Vector3>
            {
                Duration = duration ?? 1f / MathF.Abs(frequency),
                GetValue = t =>
                    offset + axis * (amplitude * MathF.Sin(MathF.Tau * frequency * t + phase))
            };
        }

        public static ComputeFunction<Vector3> Jump(
            float baseY,
            Vector3 direction,
            float intensity = 1f,
            float gravity = 9.81f)
        {
            return JumpImpulse(baseY, Vector3.Normalize(direction) * intensity, gravity);
        }

        public static ComputeFunction<Vector3> JumpImpulse(
            float baseY,
            Vector3 impulse,
            float gravity = 9.81f)
        {
            var duration = 2f * impulse.Y / gravity;
            var halfGravity = gravity * 0.5f;

            return new ComputeFunction<Vector3>
            {
                Duration = duration,
                GetValue = t => new Vector3(
                    impulse.X * t,
                    baseY + impulse.Y * t - halfGravity * t * t,
                    impulse.Z * t)
            };
        }
    }
}