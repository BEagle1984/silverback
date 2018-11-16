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

        protected override void ApplyPolicyImpl(IEnvelope envelope, Action<IEnvelope> handler, Exception exception)
            => _logger.LogWarning(
                $"The message '{envelope.Message.Id}' couldn't be successfully " +
                $"processed and will be skipped.");
    }
}