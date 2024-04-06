using CanvasUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace XrEditor
{
    public class ActionView : BaseView, IToolbarItem
    {
        private bool _isActive;
        private bool _isEnabled;
        private IconView? _icon;

        public ActionView()
        {
            _isEnabled = true;
        }

        public ActionView(Action action)
        {
            Execute = new Command(action);
            _isEnabled = true;   
        }

        public ICommand? Execute { get; set; }

        public IconView? Icon
        {
            get => _icon;
            set
            {
                if (_icon == value)
                    return;
                _icon = value;
                OnPropertyChanged(nameof(Icon));
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value)
                    return;
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive == value) 
                    return;
                _isActive = value;
                OnPropertyChanged(nameof(IsActive));    
            }
        }
    }
}
