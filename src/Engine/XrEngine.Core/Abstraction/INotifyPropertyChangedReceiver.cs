namespace XrEngine
{
    public interface INotifyPropertyChangedReceiver
    {
        void OnPropertyChanged(string name, object? value);
    }
}
