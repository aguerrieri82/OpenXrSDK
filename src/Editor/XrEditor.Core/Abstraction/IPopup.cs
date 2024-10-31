using XrMath;

namespace XrEditor
{
    public interface IPopup : IWindow
    {

        Task<ActionView?> ShowAsync();



    }
}
