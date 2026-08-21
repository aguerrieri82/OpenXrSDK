
using System.ComponentModel;
using System.Runtime.CompilerServices;
using XrEngine;

namespace XrEditor
{
    public abstract class BaseView : INotifyPropertyChanged
    {
        protected static IMainDispatcher _mainDispatcher = Context.Require<IMainDispatcher>();

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

        protected DispatcherSwitch UiThread => _mainDispatcher.Switch;
    }
}
