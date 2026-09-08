// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

// This file provides backward compatibility getter methods and MessageWith* classes for the Message class.
// These methods allow developers migrating from the old SDK to continue using
// the familiar Get*() pattern to retrieve typed data from messages.
// The MessageWith* classes provide type aliases for backward compatibility with code that
// explicitly references these types (e.g., MessageWithUser, MessageWithAchievementDefinitions).

namespace Oculus.Platform
{
    using System;
    using Oculus.Platform.Models;

    /// <summary>
    /// Partial class containing backward compatibility getter methods for Message&lt;T&gt;.
    /// These methods attempt to cast the typed Data to the requested type.
    /// </summary>
    public partial class Message<T>
    {
        public override AchievementDefinitionList GetAchievementDefinitions() => Data as AchievementDefinitionList;
        public override AchievementProgressList GetAchievementProgressList() => Data as AchievementProgressList;
        public override AchievementUpdate GetAchievementUpdate() => Data as AchievementUpdate;
        public override AppDownloadProgressResult GetAppDownloadProgressResult() => Data as AppDownloadProgressResult;
        public override AppDownloadResult GetAppDownloadResult() => Data as AppDownloadResult;
        public override ApplicationInviteList GetApplicationInviteList() => Data as ApplicationInviteList;
        public override ApplicationVersion GetApplicationVersion() => Data as ApplicationVersion;
        public override AssetDetails GetAssetDetails() => Data as AssetDetails;
        public override AssetDetailsList GetAssetDetailsList() => Data as AssetDetailsList;
        public override AssetFileDeleteResult GetAssetFileDeleteResult() => Data as AssetFileDeleteResult;
        public override AssetFileDownloadCancelResult GetAssetFileDownloadCancelResult() => Data as AssetFileDownloadCancelResult;
        public override AssetFileDownloadResult GetAssetFileDownloadResult() => Data as AssetFileDownloadResult;
        public override AssetFileDownloadUpdate GetAssetFileDownloadUpdate() => Data as AssetFileDownloadUpdate;
        public override AvatarEditorResult GetAvatarEditorResult() => Data as AvatarEditorResult;
        public override BlockedUserList GetBlockedUserList() => Data as BlockedUserList;
        public override Challenge GetChallenge() => Data as Challenge;
        public override ChallengeEntryList GetChallengeEntryList() => Data as ChallengeEntryList;
        public override ChallengeList GetChallengeList() => Data as ChallengeList;
        public override DestinationList GetDestinationList() => Data as DestinationList;
        public override GroupPresenceJoinIntent GetGroupPresenceJoinIntent() => Data as GroupPresenceJoinIntent;
        public override GroupPresenceLeaveIntent GetGroupPresenceLeaveIntent() => Data as GroupPresenceLeaveIntent;
        public override InvitePanelResultInfo GetInvitePanelResultInfo() => Data as InvitePanelResultInfo;
        public override LaunchBlockFlowResult GetLaunchBlockFlowResult() => Data as LaunchBlockFlowResult;
        public override LaunchFriendRequestFlowResult GetLaunchFriendRequestFlowResult() => Data as LaunchFriendRequestFlowResult;
        public override LaunchInvitePanelFlowResult GetLaunchInvitePanelFlowResult() => Data as LaunchInvitePanelFlowResult;
        public override LaunchReportFlowResult GetLaunchReportFlowResult() => Data as LaunchReportFlowResult;
        public override LaunchUnblockFlowResult GetLaunchUnblockFlowResult() => Data as LaunchUnblockFlowResult;
        public override bool GetLeaderboardDidUpdate() => Data is bool b && b;
        public override LeaderboardEntryList GetLeaderboardEntryList() => Data as LeaderboardEntryList;
        public override LeaderboardList GetLeaderboardList() => Data as LeaderboardList;
        public override LinkedAccountList GetLinkedAccountList() => Data as LinkedAccountList;
        public override LivestreamingStartResult GetLivestreamingStartResult() => Data as LivestreamingStartResult;
        public override LivestreamingStatus GetLivestreamingStatus() => Data as LivestreamingStatus;
        public override MicrophoneAvailabilityState GetMicrophoneAvailabilityState() => Data as MicrophoneAvailabilityState;
        public override NetSyncConnection GetNetSyncConnection() => Data as NetSyncConnection;
        public override NetSyncSessionList GetNetSyncSessionList() => Data as NetSyncSessionList;
        public override NetSyncSessionsChangedNotification GetNetSyncSessionsChangedNotification() => Data as NetSyncSessionsChangedNotification;
        public override NetSyncSetSessionPropertyResult GetNetSyncSetSessionPropertyResult() => Data as NetSyncSetSessionPropertyResult;
        public override NetSyncVoipAttenuationValueList GetNetSyncVoipAttenuationValueList() => Data as NetSyncVoipAttenuationValueList;
        public override OrgScopedID GetOrgScopedID() => Data as OrgScopedID;
        public override Party GetParty() => Data as Party;
        public override PartyID GetPartyID() => Data as PartyID;
        public override PartyUpdateNotification GetPartyUpdateNotification() => Data as PartyUpdateNotification;
        public override PlatformInitialize GetPlatformInitialize() => Data as PlatformInitialize;
        public override ProductList GetProductList() => Data as ProductList;
        public override Purchase GetPurchase() => Data as Purchase;
        public override PurchaseList GetPurchaseList() => Data as PurchaseList;
        public override PushNotificationResult GetPushNotificationResult() => Data as PushNotificationResult;
        public override RejoinDialogResult GetRejoinDialogResult() => Data as RejoinDialogResult;
        public override SdkAccountList GetSdkAccountList() => Data as SdkAccountList;
        public override SendInvitesResult GetSendInvitesResult() => Data as SendInvitesResult;
        public override ShareMediaResult GetShareMediaResult() => Data as ShareMediaResult;
        public override string GetString() => Data as string;
        public override SystemVoipState GetSystemVoipState() => Data as SystemVoipState;
        public override User GetUser() => Data as User;
        public override UserAccountAgeCategory GetUserAccountAgeCategory() => Data as UserAccountAgeCategory;
        public override UserCapabilityList GetUserCapabilityList() => Data as UserCapabilityList;
        public override UserList GetUserList() => Data as UserList;
        public override UserProof GetUserProof() => Data as UserProof;
    }

    /// <summary>
    /// Partial class containing backward compatibility getter methods for the base Message class.
    /// These methods return null/false by default since the base Message has no typed data.
    /// </summary>
    public partial class Message
    {
        /// <summary>
        /// Deprecated: use Request.RunCallbacks() to dispatch pending messages.
        /// </summary>
        [Obsolete("Use Request.RunCallbacks() to dispatch pending messages.", false)]
        public static Message PopMessage()
        {
            PlatformLog.LogWarning("Use Request.RunCallbacks() to dispatch pending messages.");
            return null;
        }

        public virtual AchievementDefinitionList GetAchievementDefinitions() => null;
        public virtual AchievementProgressList GetAchievementProgressList() => null;
        public virtual AchievementUpdate GetAchievementUpdate() => null;
        public virtual AppDownloadProgressResult GetAppDownloadProgressResult() => null;
        public virtual AppDownloadResult GetAppDownloadResult() => null;
        public virtual ApplicationInviteList GetApplicationInviteList() => null;
        public virtual ApplicationVersion GetApplicationVersion() => null;
        public virtual AssetDetails GetAssetDetails() => null;
        public virtual AssetDetailsList GetAssetDetailsList() => null;
        public virtual AssetFileDeleteResult GetAssetFileDeleteResult() => null;
        public virtual AssetFileDownloadCancelResult GetAssetFileDownloadCancelResult() => null;
        public virtual AssetFileDownloadResult GetAssetFileDownloadResult() => null;
        public virtual AssetFileDownloadUpdate GetAssetFileDownloadUpdate() => null;
        public virtual AvatarEditorResult GetAvatarEditorResult() => null;
        public virtual BlockedUserList GetBlockedUserList() => null;
        public virtual Challenge GetChallenge() => null;
        public virtual ChallengeEntryList GetChallengeEntryList() => null;
        public virtual ChallengeList GetChallengeList() => null;
        public virtual DestinationList GetDestinationList() => null;
        public virtual GroupPresenceJoinIntent GetGroupPresenceJoinIntent() => null;
        public virtual GroupPresenceLeaveIntent GetGroupPresenceLeaveIntent() => null;
        public virtual InvitePanelResultInfo GetInvitePanelResultInfo() => null;
        public virtual LaunchBlockFlowResult GetLaunchBlockFlowResult() => null;
        public virtual LaunchFriendRequestFlowResult GetLaunchFriendRequestFlowResult() => null;
        public virtual LaunchInvitePanelFlowResult GetLaunchInvitePanelFlowResult() => null;
        public virtual LaunchReportFlowResult GetLaunchReportFlowResult() => null;
        public virtual LaunchUnblockFlowResult GetLaunchUnblockFlowResult() => null;
        public virtual bool GetLeaderboardDidUpdate() => false;
        public virtual LeaderboardEntryList GetLeaderboardEntryList() => null;
        public virtual LeaderboardList GetLeaderboardList() => null;
        public virtual LinkedAccountList GetLinkedAccountList() => null;
        public virtual LivestreamingStartResult GetLivestreamingStartResult() => null;
        public virtual LivestreamingStatus GetLivestreamingStatus() => null;
        public virtual MicrophoneAvailabilityState GetMicrophoneAvailabilityState() => null;
        public virtual NetSyncConnection GetNetSyncConnection() => null;
        public virtual NetSyncSessionList GetNetSyncSessionList() => null;
        public virtual NetSyncSessionsChangedNotification GetNetSyncSessionsChangedNotification() => null;
        public virtual NetSyncSetSessionPropertyResult GetNetSyncSetSessionPropertyResult() => null;
        public virtual NetSyncVoipAttenuationValueList GetNetSyncVoipAttenuationValueList() => null;
        public virtual OrgScopedID GetOrgScopedID() => null;
        public virtual Party GetParty() => null;
        public virtual PartyID GetPartyID() => null;
        public virtual PartyUpdateNotification GetPartyUpdateNotification() => null;
        public virtual PlatformInitialize GetPlatformInitialize() => null;
        public virtual ProductList GetProductList() => null;
        public virtual Purchase GetPurchase() => null;
        public virtual PurchaseList GetPurchaseList() => null;
        public virtual PushNotificationResult GetPushNotificationResult() => null;
        public virtual RejoinDialogResult GetRejoinDialogResult() => null;
        public virtual SdkAccountList GetSdkAccountList() => null;
        public virtual SendInvitesResult GetSendInvitesResult() => null;
        public virtual ShareMediaResult GetShareMediaResult() => null;
        public virtual string GetString() => null;
        public virtual SystemVoipState GetSystemVoipState() => null;
        public virtual User GetUser() => null;
        public virtual UserAccountAgeCategory GetUserAccountAgeCategory() => null;
        public virtual UserCapabilityList GetUserCapabilityList() => null;
        public virtual UserList GetUserList() => null;
        public virtual UserProof GetUserProof() => null;
    }

    #region Backward Compatibility MessageWith* Classes
    // These classes provide backward compatibility for code that explicitly references
    // the old MessageWith* types. They are simple type aliases to Message<T>.

    public class MessageWithAchievementDefinitions : Message<AchievementDefinitionList>
    {
        public MessageWithAchievementDefinitions(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithAchievementProgressList : Message<AchievementProgressList>
    {
        public MessageWithAchievementProgressList(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithAchievementUpdate : Message<AchievementUpdate>
    {
        public MessageWithAchievementUpdate(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithAppDownloadProgressResult : Message<AppDownloadProgressResult>
    {
        public MessageWithAppDownloadProgressResult(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithAppDownloadResult : Message<AppDownloadResult>
    {
        public MessageWithAppDownloadResult(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithApplicationInviteList : Message<ApplicationInviteList>
    {
        public MessageWithApplicationInviteList(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithApplicationVersion : Message<ApplicationVersion>
    {
        public MessageWithApplicationVersion(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithAssetDetails : Message<AssetDetails>
    {
        public MessageWithAssetDetails(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithAssetDetailsList : Message<AssetDetailsList>
    {
        public MessageWithAssetDetailsList(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithAssetFileDeleteResult : Message<AssetFileDeleteResult>
    {
        public MessageWithAssetFileDeleteResult(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithAssetFileDownloadCancelResult : Message<AssetFileDownloadCancelResult>
    {
        public MessageWithAssetFileDownloadCancelResult(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithAssetFileDownloadResult : Message<AssetFileDownloadResult>
    {
        public MessageWithAssetFileDownloadResult(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithAssetFileDownloadUpdate : Message<AssetFileDownloadUpdate>
    {
        public MessageWithAssetFileDownloadUpdate(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithAvatarEditorResult : Message<AvatarEditorResult>
    {
        public MessageWithAvatarEditorResult(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithBlockedUserList : Message<BlockedUserList>
    {
        public MessageWithBlockedUserList(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithChallenge : Message<Challenge>
    {
        public MessageWithChallenge(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithChallengeList : Message<ChallengeList>
    {
        public MessageWithChallengeList(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithChallengeEntryList : Message<ChallengeEntryList>
    {
        public MessageWithChallengeEntryList(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithDestinationList : Message<DestinationList>
    {
        public MessageWithDestinationList(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithGroupPresenceJoinIntent : Message<GroupPresenceJoinIntent>
    {
        public MessageWithGroupPresenceJoinIntent(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithGroupPresenceLeaveIntent : Message<GroupPresenceLeaveIntent>
    {
        public MessageWithGroupPresenceLeaveIntent(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithInvitePanelResultInfo : Message<InvitePanelResultInfo>
    {
        public MessageWithInvitePanelResultInfo(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithLaunchBlockFlowResult : Message<LaunchBlockFlowResult>
    {
        public MessageWithLaunchBlockFlowResult(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithLaunchFriendRequestFlowResult : Message<LaunchFriendRequestFlowResult>
    {
        public MessageWithLaunchFriendRequestFlowResult(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithLaunchInvitePanelFlowResult : Message<LaunchInvitePanelFlowResult>
    {
        public MessageWithLaunchInvitePanelFlowResult(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithLaunchReportFlowResult : Message<LaunchReportFlowResult>
    {
        public MessageWithLaunchReportFlowResult(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithLaunchUnblockFlowResult : Message<LaunchUnblockFlowResult>
    {
        public MessageWithLaunchUnblockFlowResult(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithLeaderboardList : Message<LeaderboardList>
    {
        public MessageWithLeaderboardList(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithLeaderboardEntryList : Message<LeaderboardEntryList>
    {
        public MessageWithLeaderboardEntryList(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithLeaderboardDidUpdate : Message<bool>
    {
        public MessageWithLeaderboardDidUpdate(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithLinkedAccountList : Message<LinkedAccountList>
    {
        public MessageWithLinkedAccountList(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithLivestreamingStartResult : Message<LivestreamingStartResult>
    {
        public MessageWithLivestreamingStartResult(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithLivestreamingStatus : Message<LivestreamingStatus>
    {
        public MessageWithLivestreamingStatus(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithMicrophoneAvailabilityState : Message<MicrophoneAvailabilityState>
    {
        public MessageWithMicrophoneAvailabilityState(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithNetSyncConnection : Message<NetSyncConnection>
    {
        public MessageWithNetSyncConnection(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithNetSyncSessionList : Message<NetSyncSessionList>
    {
        public MessageWithNetSyncSessionList(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithNetSyncSessionsChangedNotification : Message<NetSyncSessionsChangedNotification>
    {
        public MessageWithNetSyncSessionsChangedNotification(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithNetSyncSetSessionPropertyResult : Message<NetSyncSetSessionPropertyResult>
    {
        public MessageWithNetSyncSetSessionPropertyResult(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithNetSyncVoipAttenuationValueList : Message<NetSyncVoipAttenuationValueList>
    {
        public MessageWithNetSyncVoipAttenuationValueList(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithOrgScopedID : Message<OrgScopedID>
    {
        public MessageWithOrgScopedID(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithParty : Message<Party>
    {
        public MessageWithParty(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithPartyUnderCurrentParty : Message<Party>
    {
        public MessageWithPartyUnderCurrentParty(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithPartyID : Message<PartyID>
    {
        public MessageWithPartyID(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithPartyUpdateNotification : Message<PartyUpdateNotification>
    {
        public MessageWithPartyUpdateNotification(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithProductList : Message<ProductList>
    {
        public MessageWithProductList(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithPurchase : Message<Purchase>
    {
        public MessageWithPurchase(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithPurchaseList : Message<PurchaseList>
    {
        public MessageWithPurchaseList(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithPushNotificationResult : Message<PushNotificationResult>
    {
        public MessageWithPushNotificationResult(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithRejoinDialogResult : Message<RejoinDialogResult>
    {
        public MessageWithRejoinDialogResult(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithSdkAccountList : Message<SdkAccountList>
    {
        public MessageWithSdkAccountList(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithSendInvitesResult : Message<SendInvitesResult>
    {
        public MessageWithSendInvitesResult(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithShareMediaResult : Message<ShareMediaResult>
    {
        public MessageWithShareMediaResult(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithString : Message<string>
    {
        public MessageWithString(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithSystemVoipState : Message<SystemVoipState>
    {
        public MessageWithSystemVoipState(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithUser : Message<User>
    {
        public MessageWithUser(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithUserAccountAgeCategory : Message<UserAccountAgeCategory>
    {
        public MessageWithUserAccountAgeCategory(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithUserList : Message<UserList>
    {
        public MessageWithUserList(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithUserCapabilityList : Message<UserCapabilityList>
    {
        public MessageWithUserCapabilityList(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithUserProof : Message<UserProof>
    {
        public MessageWithUserProof(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    public class MessageWithPlatformInitialize : Message<PlatformInitialize>
    {
        public MessageWithPlatformInitialize(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
            : base(requestID, sessionID, cookie, data, status) { }
    }

    #endregion
}
