// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System;
using Oculus.Platform.Models;
namespace Oculus.Platform
{
    public static partial class ApplicationLifecycle
    {
        /// <summary>
        /// Returns information about how the application was started.
        ///
        /// <para>
        /// <b>Note:</b> This synchronous method blocks the calling thread while waiting for the request to complete.
        /// For better performance, especially on the main thread, consider using <see cref="GetLaunchDetailsRequest"/> instead.
        /// </para>
        ///
        /// <para><b>Recommended Usage with async/await:</b></para>
        /// <code>
        /// async void GetLaunchDetailsAsync()
        /// {
        ///     var message = await ApplicationLifecycle.GetLaunchDetailsRequest();
        ///     if (!message.IsError)
        ///     {
        ///         LaunchDetails launchDetails = message.Data;
        ///         // Use launchDetails...
        ///     }
        /// }
        /// </code>
        ///
        /// <para><b>Recommended Usage with OnComplete callback:</b></para>
        /// <code>
        /// ApplicationLifecycle.GetLaunchDetailsRequest().OnComplete((Message&lt;LaunchDetails&gt; message) =>
        /// {
        ///     if (!message.IsError)
        ///     {
        ///         LaunchDetails launchDetails = message.Data;
        ///         // Use launchDetails...
        ///     }
        /// });
        /// </code>
        /// </summary>
        /// <returns>The launch details for the current application.</returns>
        public static LaunchDetails GetLaunchDetails()
        {
            PlatformLog.LogWarning("ApplicationLifecycle.GetLaunchDetails() is deprecated. Please use GetLaunchDetailsRequest() with async/await or OnComplete callback instead.");
            return ClientExtensionUtils.ExecuteAndWait(GetLaunchDetailsRequest(), "ApplicationLifecycle.GetLaunchDetails");
        }

        /// <summary>
        /// Logs the result of a deeplink launch.
        ///
        /// <para>
        /// <b>Note:</b> This synchronous method blocks the calling thread while waiting for the request to complete.
        /// For better performance, especially on the main thread, consider using <see cref="LogDeeplinkResultRequest"/> instead.
        /// </para>
        ///
        /// <para><b>Recommended Usage with async/await:</b></para>
        /// <code>
        /// async void LogDeeplinkResultAsync()
        /// {
        ///     var message = await ApplicationLifecycle.LogDeeplinkResultRequest(trackingId, result);
        ///     if (!message.IsError)
        ///     {
        ///         // Successfully logged deeplink result
        ///     }
        /// }
        /// </code>
        ///
        /// <para><b>Recommended Usage with OnComplete callback:</b></para>
        /// <code>
        /// ApplicationLifecycle.LogDeeplinkResultRequest(trackingId, result).OnComplete((Message message) =>
        /// {
        ///     if (!message.IsError)
        ///     {
        ///         // Successfully logged deeplink result
        ///     }
        /// });
        /// </code>
        /// </summary>
        /// <param name="trackingId">The tracking ID from the launch details.</param>
        /// <param name="result">The result of the deeplink launch.</param>
        public static void LogDeeplinkResult(string trackingId, LaunchResult result)
        {
            PlatformLog.LogWarning("ApplicationLifecycle.LogDeeplinkResult() is deprecated. Please use LogDeeplinkResultRequest() with async/await or OnComplete callback instead.");
            ClientExtensionUtils.ExecuteAndWait(LogDeeplinkResultRequest(trackingId, result), "ApplicationLifecycle.LogDeeplinkResult");
        }
    }
}
