using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine;

namespace XrEditor
{
    public class ComponentHeaderView : BaseView
    {
        IComponent _component;

        public ComponentHeaderView(IComponent component)
        {
            _component = component; 
        }

        public bool IsEnabled
        {
            get => _component.IsEnabled;
            set
            {
                _component .IsEnabled = value;  
                OnPropertyChanged(nameof(IsEnabled));   
            }
        }

        public string? Name { get; set; }

        public IconView? Icon { get; set; }
    }
}
