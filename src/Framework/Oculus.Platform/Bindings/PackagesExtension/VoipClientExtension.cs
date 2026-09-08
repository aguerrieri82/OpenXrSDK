// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System;
using Oculus.Platform.Models;
namespace Oculus.Platform
{
    public static partial class Voip
    {
        /// <summary>
        /// Attempts to establish a Voip session with the specified user.
        /// A message of type Message::MessageType::Notification_Voip_StateChange will be posted when the session is established.
        /// This function can be safely called from any thread.
        ///
        /// <para>
        /// <b>Note:</b> This synchronous method blocks the calling thread while waiting for the request to complete.
        /// For better performance, especially on the main thread, consider using <see cref="StartRequest"/> instead.
        /// </para>
        ///
        /// <para><b>Recommended Usage with async/await:</b></para>
        /// <code>
        /// async void StartVoipAsync(UInt64 userID)
        /// {
        ///     var message = await Voip.StartRequest(userID);
        ///     if (!message.IsError)
        ///     {
        ///         // Voip session started successfully
        ///     }
        /// }
        /// </code>
        ///
        /// <para><b>Recommended Usage with OnComplete callback:</b></para>
        /// <code>
        /// Voip.StartRequest(userID).OnComplete((Message message) =>
        /// {
        ///     if (!message.IsError)
        ///     {
        ///         // Voip session started successfully
        ///     }
        /// });
        /// </code>
        /// </summary>
        /// <param name="userID">The ID of the user to establish a Voip session with.</param>
        public static void Start(UInt64 userID)
        {
            PlatformLog.LogWarning("Voip.Start() is deprecated. Please use Voip.StartRequest() with async/await or OnComplete callback instead.");
            ClientExtensionUtils.ExecuteAndWait(StartRequest(userID), "Voip.Start");
        }

        /// <summary>
        /// Accepts a Voip connection from a given user.
        ///
        /// <para>
        /// <b>Note:</b> This synchronous method blocks the calling thread while waiting for the request to complete.
        /// For better performance, especially on the main thread, consider using <see cref="AcceptRequest"/> instead.
        /// </para>
        ///
        /// <para><b>Recommended Usage with async/await:</b></para>
        /// <code>
        /// async void AcceptVoipAsync(UInt64 userID)
        /// {
        ///     var message = await Voip.AcceptRequest(userID);
        ///     if (!message.IsError)
        ///     {
        ///         // Voip connection accepted successfully
        ///     }
        /// }
        /// </code>
        ///
        /// <para><b>Recommended Usage with OnComplete callback:</b></para>
        /// <code>
        /// Voip.AcceptRequest(userID).OnComplete((Message message) =>
        /// {
        ///     if (!message.IsError)
        ///     {
        ///         // Voip connection accepted successfully
        ///     }
        /// });
        /// </code>
        /// </summary>
        /// <param name="userID">The ID of the user whose Voip connection to accept.</param>
        public static void Accept(UInt64 userID)
        {
            PlatformLog.LogWarning("Voip.Accept() is deprecated. Please use Voip.AcceptRequest() with async/await or OnComplete callback instead.");
            ClientExtensionUtils.ExecuteAndWait(AcceptRequest(userID), "Voip.Accept");
        }

        /// <summary>
        /// Terminates a Voip session with the specified user.
        /// Note that Voip.SetMicrophoneMuted should be used to temporarily stop sending audio;
        /// stopping and restarting a Voip session after tearing it down may be an expensive operation.
        /// This function can be safely called from any thread.
        ///
        /// <para>
        /// <b>Note:</b> This synchronous method blocks the calling thread while waiting for the request to complete.
        /// For better performance, especially on the main thread, consider using <see cref="StopRequest"/> instead.
        /// </para>
        ///
        /// <para><b>Recommended Usage with async/await:</b></para>
        /// <code>
        /// async void StopVoipAsync(UInt64 userID)
        /// {
        ///     var message = await Voip.StopRequest(userID);
        ///     if (!message.IsError)
        ///     {
        ///         // Voip session stopped successfully
        ///     }
        /// }
        /// </code>
        ///
        /// <para><b>Recommended Usage with OnComplete callback:</b></para>
        /// <code>
        /// Voip.StopRequest(userID).OnComplete((Message message) =>
        /// {
        ///     if (!message.IsError)
        ///     {
        ///         // Voip session stopped successfully
        ///     }
        /// });
        /// </code>
        /// </summary>
        /// <param name="userID">The ID of the user to terminate the Voip session with.</param>
        public static void Stop(UInt64 userID)
        {
            PlatformLog.LogWarning("Voip.Stop() is deprecated. Please use Voip.StopRequest() with async/await or OnComplete callback instead.");
            ClientExtensionUtils.ExecuteAndWait(StopRequest(userID), "Voip.Stop");
        }

        /// <summary>
        /// Enables or disables the local microphone. When muted, the microphone will not transmit any audio.
        /// Voip connections are unaffected by this state. New connections can be established or closed
        /// whether the microphone is muted or not. This can be used to implement push-to-talk, or a local
        /// mute button. The default state is unmuted.
        /// This function can be safely called from any thread.
        ///
        /// <para>
        /// <b>Note:</b> This synchronous method blocks the calling thread while waiting for the request to complete.
        /// For better performance, especially on the main thread, consider using <see cref="SetMicrophoneMutedRequest"/> instead.
        /// </para>
        ///
        /// <para><b>Recommended Usage with async/await:</b></para>
        /// <code>
        /// async void SetMicMutedAsync(VoipMuteState state)
        /// {
        ///     var message = await Voip.SetMicrophoneMutedRequest(state);
        ///     if (!message.IsError)
        ///     {
        ///         // Microphone mute state set successfully
        ///     }
        /// }
        /// </code>
        ///
        /// <para><b>Recommended Usage with OnComplete callback:</b></para>
        /// <code>
        /// Voip.SetMicrophoneMutedRequest(state).OnComplete((Message message) =>
        /// {
        ///     if (!message.IsError)
        ///     {
        ///         // Microphone mute state set successfully
        ///     }
        /// });
        /// </code>
        /// </summary>
        /// <param name="state">The mute state to set for the microphone.</param>
        public static void SetMicrophoneMuted(VoipMuteState state)
        {
            PlatformLog.LogWarning("Voip.SetMicrophoneMuted() is deprecated. Please use Voip.SetMicrophoneMutedRequest() with async/await or OnComplete callback instead.");
            ClientExtensionUtils.ExecuteAndWait(SetMicrophoneMutedRequest(state), "Voip.SetMicrophoneMuted");
        }

        /// <summary>
        /// Sets options for newly created Voip connections. Existing connections will continue to use
        /// their current settings until they are destroyed and recreated.
        ///
        /// <para>
        /// <b>Note:</b> This synchronous method blocks the calling thread while waiting for the request to complete.
        /// For better performance, especially on the main thread, consider using <see cref="SetNewConnectionOptionsRequest"/> instead.
        /// </para>
        ///
        /// <para><b>Recommended Usage with async/await:</b></para>
        /// <code>
        /// async void SetConnectionOptionsAsync(VoipOptions voipOptions)
        /// {
        ///     var message = await Voip.SetNewConnectionOptionsRequest(voipOptions);
        ///     if (!message.IsError)
        ///     {
        ///         // Connection options set successfully
        ///     }
        /// }
        /// </code>
        ///
        /// <para><b>Recommended Usage with OnComplete callback:</b></para>
        /// <code>
        /// Voip.SetNewConnectionOptionsRequest(voipOptions).OnComplete((Message message) =>
        /// {
        ///     if (!message.IsError)
        ///     {
        ///         // Connection options set successfully
        ///     }
        /// });
        /// </code>
        /// </summary>
        /// <param name="voipOptions">The options to set for new Voip connections.</param>
        public static void SetNewConnectionOptions(VoipOptions voipOptions)
        {
            PlatformLog.LogWarning("Voip.SetNewConnectionOptions() is deprecated. Please use Voip.SetNewConnectionOptionsRequest() with async/await or OnComplete callback instead.");
            ClientExtensionUtils.ExecuteAndWait(SetNewConnectionOptionsRequest(voipOptions), "Voip.SetNewConnectionOptions");
        }

        /// <summary>
        /// Returns the current system voip microphone muted state.
        ///
        /// <para>
        /// <b>Note:</b> This synchronous method blocks the calling thread while waiting for the request to complete.
        /// For better performance, especially on the main thread, consider using <see cref="GetSystemVoipMicrophoneMutedRequest"/> instead.
        /// </para>
        ///
        /// <para><b>Recommended Usage with async/await:</b></para>
        /// <code>
        /// async void GetSystemVoipMicrophoneMutedAsync()
        /// {
        ///     var message = await Voip.GetSystemVoipMicrophoneMutedRequest();
        ///     if (!message.IsError)
        ///     {
        ///         VoipMuteState muteState = message.Data;
        ///         // Use muteState...
        ///     }
        /// }
        /// </code>
        ///
        /// <para><b>Recommended Usage with OnComplete callback:</b></para>
        /// <code>
        /// Voip.GetSystemVoipMicrophoneMutedRequest().OnComplete((Message&lt;VoipMuteState&gt; message) =>
        /// {
        ///     if (!message.IsError)
        ///     {
        ///         VoipMuteState muteState = message.Data;
        ///         // Use muteState...
        ///     }
        /// });
        /// </code>
        /// </summary>
        /// <returns>The current system voip microphone muted state.</returns>
        public static VoipMuteState GetSystemVoipMicrophoneMuted()
        {
            PlatformLog.LogWarning("Voip.GetSystemVoipMicrophoneMuted() is deprecated. Please use GetSystemVoipMicrophoneMutedRequest() with async/await or OnComplete callback instead.");
            return ClientExtensionUtils.ExecuteAndWait(GetSystemVoipMicrophoneMutedRequest(), "GetSystemVoipMicrophoneMuted");
        }

        /// <summary>
        /// Returns the current system voip status.
        ///
        /// <para>
        /// <b>Note:</b> This synchronous method blocks the calling thread while waiting for the request to complete.
        /// For better performance, especially on the main thread, consider using <see cref="GetSystemVoipStatusRequest"/> instead.
        /// </para>
        ///
        /// <para><b>Recommended Usage with async/await:</b></para>
        /// <code>
        /// async void GetSystemVoipStatusAsync()
        /// {
        ///     var message = await Voip.GetSystemVoipStatusRequest();
        ///     if (!message.IsError)
        ///     {
        ///         SystemVoipStatus status = message.Data;
        ///         // Use status...
        ///     }
        /// }
        /// </code>
        ///
        /// <para><b>Recommended Usage with OnComplete callback:</b></para>
        /// <code>
        /// Voip.GetSystemVoipStatusRequest().OnComplete((Message&lt;SystemVoipStatus&gt; message) =>
        /// {
        ///     if (!message.IsError)
        ///     {
        ///         SystemVoipStatus status = message.Data;
        ///         // Use status...
        ///     }
        /// });
        /// </code>
        /// </summary>
        /// <returns>The current system voip status.</returns>
        public static SystemVoipStatus GetSystemVoipStatus()
        {
            PlatformLog.LogWarning("Voip.GetSystemVoipStatus() is deprecated. Please use GetSystemVoipStatusRequest() with async/await or OnComplete callback instead.");
            return ClientExtensionUtils.ExecuteAndWait(GetSystemVoipStatusRequest(), "GetSystemVoipStatus");
        }

        /// <summary>
        /// Returns whether the connection to the specified peer is using discontinuous transmission (DTX).
        ///
        /// <para>
        /// <b>Note:</b> This synchronous method blocks the calling thread while waiting for the request to complete.
        /// For better performance, especially on the main thread, consider using <see cref="GetIsConnectionUsingDtxRequest"/> instead.
        /// </para>
        ///
        /// <para><b>Recommended Usage with async/await:</b></para>
        /// <code>
        /// async void GetIsConnectionUsingDtxAsync(UInt64 peerID)
        /// {
        ///     var message = await Voip.GetIsConnectionUsingDtxRequest(peerID);
        ///     if (!message.IsError)
        ///     {
        ///         VoipDtxState dtxState = message.Data;
        ///         // Use dtxState...
        ///     }
        /// }
        /// </code>
        ///
        /// <para><b>Recommended Usage with OnComplete callback:</b></para>
        /// <code>
        /// Voip.GetIsConnectionUsingDtxRequest(peerID).OnComplete((Message&lt;VoipDtxState&gt; message) =>
        /// {
        ///     if (!message.IsError)
        ///     {
        ///         VoipDtxState dtxState = message.Data;
        ///         // Use dtxState...
        ///     }
        /// });
        /// </code>
        /// </summary>
        /// <param name="peerID">The ID of the peer to check DTX status for.</param>
        /// <returns>The DTX state for the connection to the specified peer.</returns>
        public static VoipDtxState GetIsConnectionUsingDtx(UInt64 peerID)
        {
            PlatformLog.LogWarning("Voip.GetIsConnectionUsingDtx() is deprecated. Please use GetIsConnectionUsingDtxRequest() with async/await or OnComplete callback instead.");
            return ClientExtensionUtils.ExecuteAndWait(GetIsConnectionUsingDtxRequest(peerID), "GetIsConnectionUsingDtx");
        }

        /// <summary>
        /// Returns the local bitrate for the connection to the specified peer.
        ///
        /// <para>
        /// <b>Note:</b> This synchronous method blocks the calling thread while waiting for the request to complete.
        /// For better performance, especially on the main thread, consider using <see cref="GetLocalBitrateRequest"/> instead.
        /// </para>
        ///
        /// <para><b>Recommended Usage with async/await:</b></para>
        /// <code>
        /// async void GetLocalBitrateAsync(UInt64 peerID)
        /// {
        ///     var message = await Voip.GetLocalBitrateRequest(peerID);
        ///     if (!message.IsError)
        ///     {
        ///         VoipBitrate bitrate = message.Data;
        ///         // Use bitrate...
        ///     }
        /// }
        /// </code>
        ///
        /// <para><b>Recommended Usage with OnComplete callback:</b></para>
        /// <code>
        /// Voip.GetLocalBitrateRequest(peerID).OnComplete((Message&lt;VoipBitrate&gt; message) =>
        /// {
        ///     if (!message.IsError)
        ///     {
        ///         VoipBitrate bitrate = message.Data;
        ///         // Use bitrate...
        ///     }
        /// });
        /// </code>
        /// </summary>
        /// <param name="peerID">The ID of the peer to get local bitrate for.</param>
        /// <returns>The local bitrate for the connection to the specified peer.</returns>
        public static VoipBitrate GetLocalBitrate(UInt64 peerID)
        {
            PlatformLog.LogWarning("Voip.GetLocalBitrate() is deprecated. Please use GetLocalBitrateRequest() with async/await or OnComplete callback instead.");
            return ClientExtensionUtils.ExecuteAndWait(GetLocalBitrateRequest(peerID), "GetLocalBitrate");
        }

        /// <summary>
        /// Returns the remote bitrate for the connection to the specified peer.
        ///
        /// <para>
        /// <b>Note:</b> This synchronous method blocks the calling thread while waiting for the request to complete.
        /// For better performance, especially on the main thread, consider using <see cref="GetRemoteBitrateRequest"/> instead.
        /// </para>
        ///
        /// <para><b>Recommended Usage with async/await:</b></para>
        /// <code>
        /// async void GetRemoteBitrateAsync(UInt64 peerID)
        /// {
        ///     var message = await Voip.GetRemoteBitrateRequest(peerID);
        ///     if (!message.IsError)
        ///     {
        ///         VoipBitrate bitrate = message.Data;
        ///         // Use bitrate...
        ///     }
        /// }
        /// </code>
        ///
        /// <para><b>Recommended Usage with OnComplete callback:</b></para>
        /// <code>
        /// Voip.GetRemoteBitrateRequest(peerID).OnComplete((Message&lt;VoipBitrate&gt; message) =>
        /// {
        ///     if (!message.IsError)
        ///     {
        ///         VoipBitrate bitrate = message.Data;
        ///         // Use bitrate...
        ///     }
        /// });
        /// </code>
        /// </summary>
        /// <param name="peerID">The ID of the peer to get remote bitrate for.</param>
        /// <returns>The remote bitrate for the connection to the specified peer.</returns>
        public static VoipBitrate GetRemoteBitrate(UInt64 peerID)
        {
            PlatformLog.LogWarning("Voip.GetRemoteBitrate() is deprecated. Please use GetRemoteBitrateRequest() with async/await or OnComplete callback instead.");
            return ClientExtensionUtils.ExecuteAndWait(GetRemoteBitrateRequest(peerID), "GetRemoteBitrate");
        }
    }
}
