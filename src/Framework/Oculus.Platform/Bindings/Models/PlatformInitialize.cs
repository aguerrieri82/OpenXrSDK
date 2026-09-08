// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
namespace Oculus.Platform.Models
{
    public class PlatformInitialize
    {
        [JsonProperty("Result")]
        public PlatformInitializeResult Result { get; set; }
    }

    /// Describes the various results possible when attempting to initialize the
    /// platform. Anything other than ::ovrPlatformInitialize_Success should be
    /// considered a fatal error with respect to using the platform, as the
    /// platform is not guaranteed to be legitimate or work correctly.

    public enum PlatformInitializeResult : int
    {
        /// Oculus Platform SDK initialization succeeded.

        [Description("SUCCESS")]
        Success = 0,

        /// Oculus Platform SDK was not initialized.

        [Description("UNINITIALIZED")]
        Uninitialized = -1,

        /// Oculus Platform SDK failed to initialize because the pre-loaded module was
        /// on a different path than the validated library.

        [Description("PRE_LOADED")]
        PreLoaded = -2,

        /// Oculus Platform SDK files failed to load.

        [Description("FILE_INVALID")]
        FileInvalid = -3,

        /// Oculus Platform SDK failed to initialize due to an invalid signature in the
        /// signed certificate.

        [Description("SIGNATURE_INVALID")]
        SignatureInvalid = -4,

        /// Oculus Platform SDK failed to initialize due to unable to verify the
        /// application's signature during initialization.

        [Description("UNABLE_TO_VERIFY")]
        UnableToVerify = -5,

        /// There was a mismatch between the version of Oculus Platform SDK used by the
        /// application and the version installed on the Oculus user's device.

        [Description("VERSION_MISMATCH")]
        VersionMismatch = -6,

        [Description("UNKNOWN")]
        Unknown = -7,

        /// Oculus Platform SDK failed to initialize because the Oculus user had an
        /// invalid account access token.

        [Description("INVALID_CREDENTIALS")]
        InvalidCredentials = -8,

        /// Oculus Platform SDK failed to initialize because the Oculus user does not
        /// have the application entitlement.

        [Description("NOT_ENTITLED")]
        NotEntitled = -9,
    }
}
