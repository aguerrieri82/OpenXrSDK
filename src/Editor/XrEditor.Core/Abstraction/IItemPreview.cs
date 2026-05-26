using XrEditor.Abstraction;

namespace XrEditor
{
    public interface IItemPreview
    {
        Task<NativeImage?> CreatePreviewAsync();
    }
}
