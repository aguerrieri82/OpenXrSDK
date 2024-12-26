namespace XrEngine
{
    public interface ITessellation
    {
        bool UseTessellation { get; }

        bool DebugTessellation { get; set; }


        float TargetTriSize { get; set; }
    }
}
