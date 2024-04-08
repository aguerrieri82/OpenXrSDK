
using System.ComponentModel;

namespace XrEditor
{
    public abstract class BaseView : INotifyPropertyChanged
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
