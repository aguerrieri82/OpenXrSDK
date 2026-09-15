// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

namespace Oculus.Platform
{
    public static partial class Entitlements
    {
        public static Request IsUserEntitledToApplication()
        {
            return Entitlements.GetIsViewerEntitled();
        }
    }
}
