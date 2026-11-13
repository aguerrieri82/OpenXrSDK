namespace XrEngine.OpenXr
{
    public interface IShareService
    {
        Task ShareFileAsync(string filePath, string mimeType, string? title = null);
    }
}
