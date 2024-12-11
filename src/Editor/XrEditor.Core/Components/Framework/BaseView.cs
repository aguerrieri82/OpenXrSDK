
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace XrEditor
{
    public abstract class BaseView : INotifyPropertyChanged
    {
        public BaseView()
        {
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName!);
            return true;
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
