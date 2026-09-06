namespace XrEditor
{
    public interface IItemPreview
    {
        Task<NativeImage?> CreatePreviewAsync();
    }
}
