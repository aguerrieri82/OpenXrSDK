
using System.ComponentModel;
using System.Windows;

namespace Xr.Engine.Editor
{
    public abstract class BaseView : DependencyObject, INotifyPropertyChanged
    {

        public BaseView()
        {
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

    }
}
