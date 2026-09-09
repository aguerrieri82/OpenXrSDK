using global::Oculus.Platform;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace XrEngine.OpenXr.Oculus
{
    public class OculusPlatform : IDisposable
    {
        private sealed class PendingRequest
        {
            public PendingRequest(Func<IntPtr, object?> read)
            {
                Read = read;
                Completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public readonly Func<IntPtr, object?> Read;
            public readonly TaskCompletionSource<object?> Completion;

        }

        private readonly object _sync = new();
        private readonly Dictionary<ulong, PendingRequest> _requests = new();
        private readonly ManualResetEventSlim _stop = new();
        private Thread? _messageThread;
        private Exception? _pumpError;
        private string? _accessToken;
        private bool _disposed;

        public string Login(string accessToken)
        {
            _accessToken = accessToken;
            return accessToken;
        }

        public async Task<string> LoginAsync(string username, string password, ulong appId)
        {
            var init = new StandaloneNative.OculusInitParams
            {
                Type = 1, // ovrPlatformStructureType_OculusInitParams
                Email = username,
                Password = password,
                AppId = appId
            };

            var result = await RequestAsync(() => 
                StandaloneNative.ovr_Platform_InitializeStandaloneOculus(ref init),
                ReadInitializationResult)
                .ConfigureAwait(false);

            if (result != 0)
                throw new InvalidOperationException($"Oculus initialization failed: {result}.");

            _accessToken = await RequestAsync(
                StandaloneNative.ovr_User_GetAccessToken,
                ReadAccessToken).ConfigureAwait(false);

            return _accessToken!;
        }

        public async Task<string> LoginAsync(string appId)
        {
            int result;

            if (XrPlatform.IsAndroid)
            {
                var host = Context.Require<IAndroidHost>();
                result = StandaloneNative.ovr_PlatformInitializeAndroid(appId, host.NativeContext, host.NativeJniEnv);
            }
            else
                result = StandaloneNative.ovr_PlatformInitializeWindows(appId);

            if (result != 0)
                throw new InvalidOperationException($"Oculus initialization failed: {result}.");


            _accessToken = await RequestAsync(
                StandaloneNative.ovr_User_GetAccessToken,
                ReadAccessToken).ConfigureAwait(false);

            return _accessToken!;
        }

        private static int ReadInitializationResult(IntPtr message)
        {
            var initialization = StandaloneNative.ovr_Message_GetPlatformInitialize(message);
            return StandaloneNative.ovr_PlatformInitialize_GetResult(initialization);
        }

        private static string? ReadAccessToken(IntPtr message)
        {
            var token = StandaloneNative.ovr_Message_GetString(message);
            return Marshal.PtrToStringUTF8(token);
        }

        private async Task<T> RequestAsync<T>(Func<ulong> send, Func<IntPtr, T> read)
        {
            var pending = new PendingRequest(message => read(message));

            ulong requestId;

            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                if (_pumpError != null)
                    throw new InvalidOperationException("Oculus message pump stopped.", _pumpError);

                requestId = send();

                if (requestId == 0)
                    throw new InvalidOperationException("Oculus request was not started.");

                _requests.Add(requestId, pending);

                if (_messageThread == null)
                {
                    _messageThread = new Thread(PumpMessages)
                    {
                        IsBackground = true,
                        Name = "Oculus Platform messages"
                    };
                    _messageThread.Start();
                }
            }

            try
            {
                var result = await pending.Completion.Task
                    .WaitAsync(TimeSpan.FromSeconds(60))
                    .ConfigureAwait(false);

                return (T)result!;
            }
            finally
            {
                lock (_sync)
                    _requests.Remove(requestId);
            }
        }

        private void PumpMessages()
        {
            try
            {
                while (!_stop.IsSet)
                {
                    var message = StandaloneNative.ovr_PopMessage();

                    if (message == IntPtr.Zero)
                    {
                        _stop.Wait(10);
                        continue;
                    }

                    try
                    {
                        var requestId = StandaloneNative.ovr_Message_GetRequestID(message);
                        PendingRequest? pending;

                        lock (_sync)
                            _requests.Remove(requestId, out pending);

                        if (pending != null)
                            CompleteRequest(pending, message);
                    }
                    finally
                    {
                        StandaloneNative.ovr_FreeMessage(message);
                    }
                }
            }
            catch (Exception error)
            {
                lock (_sync)
                {
                    _pumpError = error;
                    FailRequests(error);
                }
            }
        }

        private static void CompleteRequest(PendingRequest pending, IntPtr message)
        {
            try
            {
                if (StandaloneNative.ovr_Message_IsError(message))
                {
                    var error = StandaloneNative.ovr_Message_GetError(message);
                    var code = StandaloneNative.ovr_Error_GetCode(error);
                    var text = Marshal.PtrToStringUTF8(StandaloneNative.ovr_Error_GetMessage(error));

                    throw new InvalidOperationException($"Oculus Platform request failed: {code}.");
                }

                pending.Completion.TrySetResult(pending.Read(message));
            }
            catch (Exception error)
            {
                pending.Completion.TrySetException(error);
            }
        }

        private void FailRequests(Exception error)
        {
            foreach (var pending in _requests.Values)
                pending.Completion.TrySetException(error);

            _requests.Clear();
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed)
                    return;

                _disposed = true;
                _stop.Set();
                FailRequests(new ObjectDisposedException(nameof(OculusPlatform)));
            }

            _messageThread?.Join();
            _stop.Dispose();

            GC.SuppressFinalize(this);
        }

        public string? AccessToken => _accessToken;
    }
}
