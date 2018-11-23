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
        private readonly ILogger<SkipMessageErrorPolicy> _logger;

        public SkipMessageErrorPolicy(ILogger<SkipMessageErrorPolicy> logger)
            : base(logger)
        {
            _logger = logger;
        }

        protected override void ApplyPolicy(IMessage message, Action<IMessage> messageHandler, Exception exception)
            => _logger.LogWarning(
                $"The message {message.GetTraceString()} couldn't be successfully " +
                $"processed and will be skipped.");
    }
}