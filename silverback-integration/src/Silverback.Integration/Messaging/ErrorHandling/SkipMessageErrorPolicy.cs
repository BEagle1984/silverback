using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// This policy simply skips the message that failed to be processed.
    /// </summary>
    public class SkipMessageErrorPolicy : ErrorPolicyBase
    {
        public SkipMessageErrorPolicy(ILogger<SkipMessageErrorPolicy> logger)
            : base(logger)
        {
        }

        public override ErrorAction HandleError(IMessage failedMessage, int retryCount, Exception exception) =>
            ErrorAction.SkipMessage;
    }
}