// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System;

namespace Oculus.Platform
{
    public partial class Message
    {
        /// Returns the message type. Deprecated: use Message&lt;T&gt;.DataType instead.
        /// Old: if (msg.Type == MessageType.Leaderboard_GetEntries) { ... }
        /// New: if (msg.DataType == typeof(LeaderboardEntryList)) { ... }
        /// The typeof(...) here is the class type of the returned model
        [Obsolete("Type checking is no longer necessary — the OnComplete callback guarantees the correct type. This property always returns Unknown. If needed, use Message<T>.DataType instead.", true)]
        public MessageType Type => MessageType.Unknown;

        /// MessageType enum for backward compatibility.
        public enum MessageType : uint
        {
            Unknown,
            AbuseReport_ReportRequestHandled = 0x4B8EFC86,
            Achievements_AddCount = 0x03E76231,
            Achievements_AddFields = 0x14AA2129,
            Achievements_GetAllDefinitions = 0x03D3458D,
            Achievements_GetAllProgress = 0x4F9FDE1D,
            Achievements_GetDefinitionsByName = 0x629101BC,
            Achievements_GetNextAchievementDefinitionArrayPage = 0x2A7DD255,
            Achievements_GetNextAchievementProgressArrayPage = 0x2F42E727,
            Achievements_GetProgressByName = 0x152663B1,
            Achievements_Unlock = 0x593CCBDD,
            ApplicationLifecycle_GetRegisteredPIDs = 0x04E5CF62,
            ApplicationLifecycle_GetSessionKey = 0x3AAF591D,
            ApplicationLifecycle_RegisterSessionKey = 0x4DB6AFF8,
            Application_CancelAppDownload = 0x7C2060DE,
            Application_CheckAppDownloadProgress = 0x5534A924,
            Application_GetVersion = 0x68670A0E,
            Application_InstallAppUpdateAndRelaunch = 0x14806B85,
            Application_LaunchOtherApp = 0x54E2D1F8,
            Application_StartAppDownload = 0x44FC006E,
            AssetFile_Delete = 0x6D5D7886,
            AssetFile_DeleteById = 0x5AE8CD52,
            AssetFile_DeleteByName = 0x420AC1CF,
            AssetFile_Download = 0x11449FC5,
            AssetFile_DownloadById = 0x2D008992,
            AssetFile_DownloadByName = 0x6336CEFA,
            AssetFile_DownloadCancel = 0x080AD3C7,
            AssetFile_DownloadCancelById = 0x51659514,
            AssetFile_DownloadCancelByName = 0x446AECFA,
            AssetFile_GetList = 0x4AFC6F74,
            AssetFile_Status = 0x02D32F60,
            AssetFile_StatusById = 0x5D955D38,
            AssetFile_StatusByName = 0x41CFDA50,
            Avatar_LaunchAvatarEditor = 0x05F1E153,
            Challenges_Create = 0x6859D641,
            Challenges_DeclineInvite = 0x568E76C0,
            Challenges_Delete = 0x264885CA,
            Challenges_Get = 0x77584EF3,
            Challenges_GetEntries = 0x121AB45F,
            Challenges_GetEntriesAfterRank = 0x08891A7F,
            Challenges_GetEntriesByIds = 0x316509DC,
            Challenges_GetList = 0x43264356,
            Challenges_GetNextChallenges = 0x5B7CA1B6,
            Challenges_GetNextEntries = 0x7F4CA0C6,
            Challenges_GetPreviousChallenges = 0x0EB4040D,
            Challenges_GetPreviousEntries = 0x78C90470,
            Challenges_Join = 0x21248069,
            Challenges_Leave = 0x296116E5,
            Challenges_UpdateInfo = 0x1175BE60,
            DeviceApplicationIntegrity_GetIntegrityToken = 0x3271ABDA,
            Entitlement_GetIsViewerEntitled = 0x186B58B1,
            GroupPresence_Clear = 0x6DAA9CC3,
            GroupPresence_GetInvitableUsers = 0x234BC3F1,
            GroupPresence_GetNextApplicationInviteArrayPage = 0x04F8C0F2,
            GroupPresence_GetSentInvites = 0x08260AB1,
            GroupPresence_LaunchInvitePanel = 0x0F9ECF9F,
            GroupPresence_LaunchMultiplayerErrorDialog = 0x2955AF24,
            GroupPresence_LaunchRejoinDialog = 0x1577036F,
            GroupPresence_LaunchRosterPanel = 0x35728882,
            GroupPresence_SendInvites = 0x0DCBD364,
            GroupPresence_Set = 0x675F5C24,
            GroupPresence_SetDeeplinkMessageOverride = 0x521ADF0D,
            GroupPresence_SetDestination = 0x4C5B268A,
            GroupPresence_SetIsJoinable = 0x2A8F1055,
            GroupPresence_SetLobbySession = 0x48FF55BE,
            GroupPresence_SetMatchSession = 0x314C84B8,
            IAP_ConsumePurchase = 0x1FBB72D9,
            IAP_GetNextProductArrayPage = 0x1BD94AAF,
            IAP_GetNextPurchaseArrayPage = 0x47570A95,
            IAP_GetProductsBySKU = 0x7E9ACAF5,
            IAP_GetViewerPurchases = 0x3A0F8419,
            IAP_GetViewerPurchasesDurableCache = 0x63599E2B,
            IAP_LaunchCheckoutFlow = 0x3F9B0D0D,
            LanguagePack_GetCurrent = 0x1F90F0D5,
            LanguagePack_SetCurrent = 0x5B4FBBE0,
            Leaderboard_Get = 0x6AD44EF8,
            Leaderboard_GetEntries = 0x5DB3474C,
            Leaderboard_GetEntriesAfterRank = 0x18378BEF,
            Leaderboard_GetEntriesByIds = 0x39607BFC,
            Leaderboard_GetNextEntries = 0x4E207CD9,
            Leaderboard_GetNextLeaderboardArrayPage = 0x35F6769B,
            Leaderboard_GetPreviousEntries = 0x4901DAC0,
            Leaderboard_WriteEntry = 0x117FC8FE,
            Leaderboard_WriteEntryWithSupplementaryMetric = 0x72C692FA,
            Media_ShareToFacebook = 0x00E38AEF,
            Notification_MarkAsRead = 0x717259E3,
            PushNotification_Register = 0x663A8B5F,
            RichPresence_Clear = 0x57B752B3,
            RichPresence_GetDestinations = 0x586F2D14,
            RichPresence_GetNextDestinationArrayPage = 0x67367F45,
            RichPresence_Set = 0x3C147509,
            UserAgeCategory_Get = 0x21CBE0C0,
            UserAgeCategory_Report = 0x2E4DD8D6,
            User_Get = 0x6BCF9E47,
            User_GetAccessToken = 0x06A85ABE,
            User_GetBlockedUsers = 0x7D201556,
            User_GetLinkedAccounts = 0x5793F456,
            User_GetLoggedInUser = 0x436F345D,
            User_GetLoggedInUserFriends = 0x587C2A8D,
            User_GetLoggedInUserManagedInfo = 0x70BA3AEE,
            User_GetNextBlockedUserArrayPage = 0x7C2AFDCB,
            User_GetNextUserArrayPage = 0x267CF743,
            User_GetNextUserCapabilityArrayPage = 0x2309F399,
            User_GetOrgScopedID = 0x18F0B01B,
            User_GetSdkAccounts = 0x67526A83,
            User_GetUserProof = 0x22810483,
            User_LaunchBlockFlow = 0x6FD62528,
            User_LaunchFriendRequestFlow = 0x0904B598,
            User_LaunchUnblockFlow = 0x14A22A97,
            Voip_GetMicrophoneAvailability = 0x744CE345,
            Voip_SetSystemVoipSuppressed = 0x453FC9AA,

            /// The user has tapped the report button in the panel that appears after
            /// pressing the Oculus button.
            Notification_AbuseReport_ReportButtonPressed = 0x24472F6C,

            /// This event is triggered when a launch intent is received, whether it's a
            /// cold or warm start. The payload contains the type of intent that was
            /// received. To obtain additional details, you should call the
            /// ApplicationLifecycle.GetLaunchDetails() function.
            Notification_ApplicationLifecycle_LaunchIntentChanged = 0x04B34CA3,

            /// Sent to indicate download progress for asset files.
            Notification_AssetFile_DownloadUpdate = 0x2FDD0CCD,

            /// Sent when the user is finished using the invite panel to send out
            /// invitations. Contains a list of invitees.
            Notification_GroupPresence_InvitationsSent = 0x679A84B6,

            /// Sent when a user has chosen to join the destination/lobby/match.
            Notification_GroupPresence_JoinIntentReceived = 0x773889F6,

            /// Sent when the user has chosen to leave the destination/lobby/match from the
            /// Oculus menu.
            Notification_GroupPresence_LeaveIntentReceived = 0x4737EA1D,

            /// Sent to indicate that more data has been read or an error occured.
            Notification_HTTP_Transfer = 0x7DD46E2F,

            /// Indicates that the livestreaming session has been updated.
            Notification_Livestreaming_StatusChange = 0x2247596E,

            /// Sent when the status of a connection has changed.
            Notification_NetSync_ConnectionStatusChanged = 0x073484CA,

            /// Sent when the list of known connected sessions has changed.
            Notification_NetSync_SessionsChanged = 0x387E7F36,

            /// Indicates that party has been updated.
            Notification_Party_PartyUpdate = 0x1D118AB2,

            /// Indicates that the current microphone availability state has been updated.
            Notification_Voip_MicrophoneAvailabilityStateUpdate = 0x3E20CB57,

            /// Sent to indicate that some part of the overall state of SystemVoip has
            /// changed.
            Notification_Voip_SystemVoipState = 0x58D254A5,

            /// Gets VR camera related WebRTC data channel messages for update.
            Notification_Vrcamera_GetDataChannelMessageUpdate = 0x6EE4F33C,

            /// Gets the surface and update action from the platform WebRTC for update.
            Notification_Vrcamera_GetSurfaceUpdate = 0x37F21084,

            Platform_InitializeWithAccessToken = 0x35692F2B,
            Platform_InitializeStandaloneOculus = 0x51F8CE0C,
            Platform_InitializeAndroidAsynchronous = 0x1AD307B4,
            Platform_InitializeWindowsAsynchronous = 0x6DA7BA8F,
            Notification_Session_InvitationsSent = 0x07F9C880,
        }
    }
}
