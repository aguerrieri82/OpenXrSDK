using System.ComponentModel;
using XrEngine;

namespace XrEditor
{
    public class PlayerView : BaseEditor<IPlayer, IPlayer>, IDisposable
    {

        public PlayerView()
        {
            PlayCommand = new Command(Play);
            StopCommand = new Command(Stop);
            PauseCommand = new Command(Pause);
            NextFrameCommand = new Command(NextFrame);
            PrevFrameCommand = new Command(PrevFrame);
            OnPropertyChanged(nameof(Length));
        }

        protected override void OnEditValueChanged(IPlayer newValue)
        {
            if (newValue is INotifyPropertyChanged notify)
                notify.PropertyChanged += OnPropertyChanged;
            base.OnEditValueChanged(newValue);
            UpdateCommands();
        }

        public void Dispose()
        {
            if (_editValue is INotifyPropertyChanged notify)
                notify.PropertyChanged -= OnPropertyChanged;

            GC.SuppressFinalize(this);
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_editValue.Length))
                OnPropertyChanged(nameof(Length));

            if (e.PropertyName == nameof(_editValue.Frame))
                OnPropertyChanged(nameof(Position));

            UpdateCommands();
        }

        public void Play()
        {
            _editValue.SetPlayState(PlayerState.Play);
            UpdateCommands();
        }

        public void Stop()
        {
            _editValue.SetPlayState(PlayerState.Stop);
            UpdateCommands();
        }

        public void Pause()
        {
            _editValue.SetPlayState(PlayerState.Pause);
            UpdateCommands();
        }

        public void NextFrame()
        {
            _editValue.Frame++;
        }

        public void PrevFrame()
        {
            _editValue.Frame--;
        }

        public int Position
        {
            get => _editValue.Frame;
            set
            {
                if (_editValue.Frame == value)
                    return;
                _editValue.Frame = value;
                OnPropertyChanged(nameof(Position));
            }
        }

        public int Length
        {
            get => _editValue.Length;
        }

        protected void UpdateCommands()
        {
            if (_editValue == null)
                return;

            PlayCommand.IsEnabled = _editValue.PlayerState != PlayerState.Play;
            PauseCommand.IsEnabled = _editValue.PlayerState == PlayerState.Play;
            NextFrameCommand.IsEnabled = Position < Length - 1 && _editValue.PlayerState != PlayerState.Play;
            PrevFrameCommand.IsEnabled = Position > 0 && _editValue.PlayerState != PlayerState.Play;
        }

        public Command PlayCommand { get; }

        public Command StopCommand { get; }

        public Command PauseCommand { get; }

        public Command NextFrameCommand { get; }

        public Command PrevFrameCommand { get; }

    }
}
