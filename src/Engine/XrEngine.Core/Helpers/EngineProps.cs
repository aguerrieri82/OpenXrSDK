namespace XrEngine.Helpers
{
    public static class EngineProps
    {
        public static readonly DynamicProp ActiveTool = new(nameof(ActiveTool));

        public static readonly DynamicProp MotionVectorPrev = new(nameof(MotionVectorPrev));

        public static readonly DynamicProp Layout = new(nameof(Layout));
        
    }
}
