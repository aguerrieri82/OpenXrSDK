namespace XrEngine.Animation
{
    public static class TimeFunctions
    {
        public static float Linear(float t, float duration) => t;

        public static float Step(float t, float duration) => t < 1f ? 0f : 1f;
    }
}
