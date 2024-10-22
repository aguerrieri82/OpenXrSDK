﻿using System.Windows;

namespace XrEditor.Components
{
    public class WindowPopup : Window, IPopup
    {
        private ActionView? _lastAction;

        public TaskCompletionSource? _onClosed;

        public WindowPopup()
        {
            Style = Application.Current.Resources["CustomWindowStyle"] as Style;
            Owner = Application.Current.MainWindow;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        protected override void OnClosed(EventArgs e)
        {
            _onClosed?.SetResult();
            _onClosed = null;

            base.OnClosed(e);
        }

        public async Task<ActionView?> ShowAsync()
        {
            var body = (ContentView)Content;

            if (body.Actions != null)
            {
                foreach (var action in body.Actions)
                {
                    var oldCommand = action.ExecuteCommand;
                    action.ExecuteCommand = new Command(() =>
                    {
                        oldCommand?.Execute(null);
                        _lastAction = action;
                        Close();
                    });
                }
            }

            Title = body.Title;

            _onClosed = new TaskCompletionSource();

            Show();

            await _onClosed.Task;

            return _lastAction;
        }
    }
}
