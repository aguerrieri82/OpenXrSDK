/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
// Native declarations ported from Meta Avatars SDK 40.0.1. Unity helpers omitted.
#nullable enable
using System;
using ovrAvatar2EntityFeatures = Oculus.Avatar2.CAPI.ovrAvatar2EntityFeatures;

namespace Oculus.Avatar2.Experimental
{
    [Flags]
    [System.Serializable]
    public enum ExperimentalEntityFeatureFlag : UInt32
    {
        EventSystem = Oculus.Avatar2.CAPI.ovrAvatar2EntityFeatures_Last << 1,
        BehaviorSystem = EventSystem << 1,
        UseRtRig = BehaviorSystem << 1,
        ConstraintSolver = UseRtRig << 1,
        DisableHeightAdjustment = ConstraintSolver << 1,
        DisableLimbLengthScalingFix = DisableHeightAdjustment << 1,
        DisableHeadsetIK = DisableLimbLengthScalingFix << 1,
        PostIntentionalityStreaming = DisableHeadsetIK << 1,
    }
}
