﻿using System.Windows;

namespace XrEditor
{
    public class MainDispatcher : IMainDispatcher
    {
        public bool IsCurrentThread => Application.Current.Dispatcher.Thread == Thread.CurrentThread;

        public async Task ExecuteAsync(Action action)
        {
            if (Application.Current == null)
                return;

            if (Application.Current.Dispatcher.Thread == Thread.CurrentThread)
                action();
            else
            {
                try
                {
                    await Application.Current.Dispatcher.InvokeAsync(action);
                }
                catch (TaskCanceledException)
                {

                }
            }

        }

        public void Execute(Action action)
        {

            if (Application.Current.Dispatcher.Thread == Thread.CurrentThread)
                action();
            else
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(action);
                }
                catch (TaskCanceledException)
                {

                }
            }

        }
    }
}
