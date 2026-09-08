// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System.Collections;
using System.Collections.Generic;
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
