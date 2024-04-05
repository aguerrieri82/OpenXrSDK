namespace CanvasUI
{
    public interface IUiPointer
    {
        int Id { get; }

        void Capture(UiElement element);

        void Release();
    }
}
