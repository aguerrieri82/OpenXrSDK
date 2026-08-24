namespace XrEngine
{
    public static class ImagesSlots
    {
        public static readonly ResourceSlot Depth = new(0, nameof(Depth));
        public static readonly ResourceSlot MotionVectors = new(1, nameof(MotionVectors));

        public static readonly SlotMask Reserved = ResourceSlot.FillMask(typeof(ImagesSlots));
    }
}