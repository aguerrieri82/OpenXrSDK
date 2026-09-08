// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<91287802664625ef5d35f7a950f01def>>
 */

namespace Oculus.Platform
{
    public class HorizonStatus
    {
        public HorizonStatus(int code, string context)
        {
            this.code = code;
            this.context = context;
        }

        public bool IsSuccess()
        {
            return code == 0;
        }

        public void ThrowIfError()
        {
            if (IsSuccess())
            {
                return;
            }

            switch (code)
            {
                case 1: // ErrorUnspecified
                    throw new HorizonException("Unspecified error occurred: " + context);
                case 2: // NotInitialized
                    throw new HorizonNotInitializedException(context);
                case 3: // InvalidId
                    throw new HorizonInvalidIdException(context);
                case 4: // EmptyQueue
                    throw new HorizonEmptyQueueException(context);
                case 5: // InternalError
                    throw new HorizonInternalErrorException(context);
                case 6: // SessionNotFound
                    throw new HorizonSessionNotFoundException(context);
                case 7: // ActiveSessionsLimit
                    throw new HorizonActiveSessionsLimitException(context);
                case 8: // RuntimeDiscoveryFailed
                    throw new HorizonRuntimeDiscoveryFailedException(context);
                case 9: // RateLimitExceeded
                    throw new HorizonRateLimitExceededException(context);
                case 10: // ProviderError
                    throw new HorizonProviderErrorException(context);
                case 11: // EntitlementFailure
                    throw new HorizonEntitlementFailureException(context);
                case 12: // RequestNotSupported
                    throw new HorizonRequestNotSupportedException(context);
                case 13: // ServiceProviderNotFound
                    throw new HorizonServiceProviderNotFoundException(context);
                case 14: // Unauthorized
                    throw new HorizonUnauthorizedException(context);
                case 15: // Forbidden
                    throw new HorizonForbiddenException(context);
                case 16: // ConnectionError
                    throw new HorizonConnectionErrorException(context);
                case 17: // SessionClosed
                    throw new HorizonSessionClosedException(context);
                case 18: // NetworkUnavailable
                    throw new HorizonNetworkUnavailableException(context);
                case 19: // InvalidConfig
                    throw new HorizonInvalidConfigException(context);
                default:
                    throw new HorizonException($"An unknown error occurred when calling {context}");
            }
        }

        public int Code()
        {
            return code;
        }

        public string GetContext()
        {
            return context;
        }

        private int code;
        private string context;
    }
}
