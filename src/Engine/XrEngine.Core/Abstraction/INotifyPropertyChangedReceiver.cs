using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine
{
    public interface INotifyPropertyChangedReceiver
    {
        void OnPropertyChanged(string name, object? value);
    }
}
