﻿using System.Windows.Input;

namespace XrEditor
{
    public class Command : ICommand
    {
        readonly Action _action;
        bool _isEnabled;

        public Command(Action action)
        {
            _action = action;
            _isEnabled = true;
        }


        public bool CanExecute(object? parameter)
        {
            return _isEnabled;
        }

        public void Execute(object? parameter)
        {
            _action();
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value)
                    return;
                _isEnabled = value;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);

            }
        }

        public event EventHandler? CanExecuteChanged;

    }
}