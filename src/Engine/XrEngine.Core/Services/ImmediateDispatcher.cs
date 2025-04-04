﻿namespace XrEngine
{
    public class ImmediateDispatcher : IDispatcher
    {
        public Task ExecuteAsync(Action action)
        {
            action();
            return Task.CompletedTask;
        }

        public Task<T> ExecuteAsync<T>(Func<T> action)
        {
            return Task.FromResult(action());
        }
    }
}
