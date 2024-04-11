using System.Windows.Input;

namespace XrEditor
{
    public class Command : ICommand
    {
        protected readonly Func<Task> _action;
        protected bool _isEnabled;
        protected bool _isExecuting;

        public Command(Func<Task> action)
        {
            _action = action;
            _isEnabled = true;
        }

        public Command(Action action)
        {
            _action = () =>
            {
                action();
                return Task.CompletedTask;
            };

            _isEnabled = true;
        }


        public bool CanExecute(object? parameter)
        {
            return _isEnabled && !_isExecuting;
        }

        public async void Execute(object? parameter)
        {
            _isExecuting = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            try
            {
                await _action();
            }
            finally
            {
                _isExecuting = false;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
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
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);

            }
        }

        public event EventHandler? CanExecuteChanged;

    }
}
