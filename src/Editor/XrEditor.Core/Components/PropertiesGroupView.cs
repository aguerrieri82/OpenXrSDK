﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace XrEditor
{
    public class PropertiesGroupView : BaseView
    {
        private bool _isCollapsed;
        private object? _header;
        private IList<PropertyView> _properties = [];
        private IList<PropertiesGroupView> _groups = [];

        public PropertiesGroupView()
        {
            ToggleCollapseCommand = new Command(() => IsCollapsed = !IsCollapsed);
        }

        public object? Header
        {
            get => _header;
            set
            {
                if (_header == value)
                    return;
                _header = value;
                OnPropertyChanged(nameof(Header));
            }
        }


        public bool IsCollapsed
        {
            get => _isCollapsed;
            set
            {
                if (_isCollapsed == value)
                    return;
                _isCollapsed = value;
                OnPropertyChanged(nameof(IsCollapsed));
            }
        }

        public IList<PropertyView> Properties
        {
            get => _properties;
            set
            {
                if (_properties == value)
                    return;
                _properties = value;
                OnPropertyChanged(nameof(Properties));
            }
        }

        public IList<PropertiesGroupView> Groups
        {
            get => _groups;
            set
            {
                if (_groups == value)
                    return;
                _groups = value;
                OnPropertyChanged(nameof(Groups));
            }
        }


        public ICommand ToggleCollapseCommand { get; }
    }
}