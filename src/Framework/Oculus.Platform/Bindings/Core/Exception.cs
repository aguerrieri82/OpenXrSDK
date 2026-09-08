// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<7f1d6a8835ba490fed3aa5344ed83c4a>>
 */

using System;

namespace Oculus.Platform
{
    public class HorizonException : Exception
    {
        public HorizonException(string message) : base(message) { }
    }

    public class HorizonNotInitializedException : HorizonException
    {
        public HorizonNotInitializedException(string context) : base($"Error: The platform is not initialized. Context: {context}.") { }
    }

    public class HorizonInvalidIdException : HorizonException
    {
        public HorizonInvalidIdException(string context) : base($"Error: Invalid ID was provided. Context: {context}.") { }
    }

    public class HorizonEmptyQueueException : HorizonException
    {
        public HorizonEmptyQueueException(string context) : base($"Error: The Message Queue being polled is empty. Context: {context}.") { }
    }

    public class HorizonInternalErrorException : HorizonException
    {
        public HorizonInternalErrorException(string context) : base($"Error: Internal error occurred. Context: {context}.") { }
    }

    public class HorizonSessionNotFoundException : HorizonException
    {
        public HorizonSessionNotFoundException(string context) : base($"Error: Session not found. Context: {context}.") { }
    }

    public class HorizonActiveSessionsLimitException : HorizonException
    {
        public HorizonActiveSessionsLimitException(string context) : base($"Error: Active sessions limit reached. Context: {context}.") { }
    }

    public class HorizonRuntimeDiscoveryFailedException : HorizonException
    {
        public HorizonRuntimeDiscoveryFailedException(string context) : base($"Error: Unable to find the Horizon Platform Service. Context: {context}.") { }
    }

    public class HorizonRateLimitExceededException : HorizonException
    {
        public HorizonRateLimitExceededException(string context) : base($"Error: Rate limit exceeded. Context: {context}.") { }
    }

    public class HorizonProviderErrorException : HorizonException
    {
        public HorizonProviderErrorException(string context) : base($"Error: Provider error occurred. Context: {context}.") { }
    }

    public class HorizonEntitlementFailureException : HorizonException
    {
        public HorizonEntitlementFailureException(string context) : base($"Error: Entitlement check failed. Context: {context}.") { }
    }

    public class HorizonRequestNotSupportedException : HorizonException
    {
        public HorizonRequestNotSupportedException(string context) : base($"Error: Request is not supported. Context: {context}.") { }
    }

    public class HorizonServiceProviderNotFoundException : HorizonException
    {
        public HorizonServiceProviderNotFoundException(string context) : base($"Error: Unable to find a service provider that can complete this operation. Context: {context}.") { }
    }

    public class HorizonUnauthorizedException : HorizonException
    {
        public HorizonUnauthorizedException(string context) : base($"Error: Unauthorized access. Context: {context}.") { }
    }

    public class HorizonForbiddenException : HorizonException
    {
        public HorizonForbiddenException(string context) : base($"Error: Access forbidden. Context: {context}.") { }
    }

    public class HorizonConnectionErrorException : HorizonException
    {
        public HorizonConnectionErrorException(string context) : base($"Error: Connection error occurred. Context: {context}.") { }
    }

    public class HorizonSessionClosedException : HorizonException
    {
        public HorizonSessionClosedException(string context) : base($"Error: Session is closed. Context: {context}.") { }
    }

    public class HorizonNetworkUnavailableException : HorizonException
    {
        public HorizonNetworkUnavailableException(string context) : base($"Error: The network is unavailable. Context: {context}.") { }
    }

    public class HorizonInvalidConfigException : HorizonException
    {
        public HorizonInvalidConfigException(string context) : base($"Error: Invalid configuration. Context: {context}.") { }
    }
}
