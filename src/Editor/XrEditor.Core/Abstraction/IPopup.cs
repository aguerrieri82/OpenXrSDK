namespace XrEditor
{
    public interface IPopup
    {
        void Close();

        Task<ActionView?> ShowAsync();
    }
}
