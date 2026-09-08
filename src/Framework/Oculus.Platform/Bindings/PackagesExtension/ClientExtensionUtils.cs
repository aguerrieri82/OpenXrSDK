// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System;
namespace Oculus.Platform
{
    /// <summary>
    /// Utility methods for client extensions to convert async requests to synchronous calls.
    /// These are primarily used to maintain backward compatibility with deprecated synchronous APIs.
    /// </summary>
    public static class ClientExtensionUtils
    {
        /// <summary>
        /// Default timeout in seconds for synchronous request operations.
        /// </summary>
        public const float DefaultTimeoutSeconds = 2f;

        /// <summary>
        /// Executes an async request synchronously by blocking until completion or timeout.
        /// This method pumps the callback queue while waiting for the response.
        ///
        /// <para>
        /// <b>Warning:</b> This method blocks the calling thread. For better performance,
        /// especially on the main thread, consider using async/await or OnComplete callbacks instead.
        /// </para>
        /// </summary>
        /// <param name="request">The async request to execute.</param>
        /// <param name="methodName">The name of the calling method (used for error logging).</param>
        public static void ExecuteAndWait(
            Request request,
            string methodName)
        {
            bool completed = false;
            Exception exception = null;

            request.OnComplete((Message msg) =>
            {
                try
                {
                    if (msg.IsError)
                    {
                        PlatformLog.LogError($"{methodName} failed: {msg.GetError().Message}");
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                }
                finally
                {
                    completed = true;
                }
            });

            var timer = System.Diagnostics.Stopwatch.StartNew();
            while (!completed)
            {
                if (timer.Elapsed.TotalSeconds > DefaultTimeoutSeconds)
                {
                    PlatformLog.LogError($"{methodName} timed out after {DefaultTimeoutSeconds} seconds");
                    break;
                }

                Callback.RunCallbacks();
            }

            if (exception != null)
            {
                throw exception;
            }
        }

        /// <summary>
        /// Executes an async request synchronously by blocking until completion or timeout,
        /// and returns the result data.
        /// This method pumps the callback queue while waiting for the response.
        ///
        /// <para>
        /// <b>Warning:</b> This method blocks the calling thread. For better performance,
        /// especially on the main thread, consider using async/await or OnComplete callbacks instead.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The type of data returned by the request.</typeparam>
        /// <param name="request">The async request to execute.</param>
        /// <param name="methodName">The name of the calling method (used for error logging).</param>
        /// <returns>The result data from the request, or default(T) if the request failed or timed out.</returns>
        public static T ExecuteAndWait<T>(
            Request<T> request,
            string methodName)
        {
            T result = default;
            bool completed = false;
            Exception exception = null;

            request.OnComplete((Message<T> msg) =>
            {
                try
                {
                    if (msg.IsError)
                    {
                        PlatformLog.LogError($"{methodName} failed: {msg.GetError().Message}");
                        result = default;
                    }
                    else
                    {
                        result = msg.Data;
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                }
                finally
                {
                    completed = true;
                }
            });

            var timer = System.Diagnostics.Stopwatch.StartNew();
            while (!completed)
            {
                if (timer.Elapsed.TotalSeconds > DefaultTimeoutSeconds)
                {
                    PlatformLog.LogError($"{methodName} timed out after {DefaultTimeoutSeconds} seconds");
                    break;
                }

                Callback.RunCallbacks();
            }

            if (exception != null)
            {
                throw exception;
            }

            return result;
        }
    }
}
