using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// This policy simply skips the message that failed to be processed.
    /// </summary>
    /// <seealso cref="Silverback.Messaging.ErrorHandling.ErrorPolicyBase" />
    public class SkipMessageErrorPolicy : ErrorPolicyBase
    {
        private ILogger _logger;

        /// <summary>
        /// Initializes the policy, binding to the specified bus.
        /// </summary>
        /// <param name="bus">The bus.</param>
        public override void Init(IBus bus)
        {
            _logger = bus.GetLoggerFactory().CreateLogger<SkipMessageErrorPolicy>();
            base.Init(bus);
        }

        /// <summary>
        /// Applies the error handling policy.
        /// </summary>
        /// <param name="envelope">The envelope containing the failed message.</param>
        /// <param name="handler">The method that was used to handle the message.</param>
        public override void ApplyPolicy(IEnvelope envelope, Action<IEnvelope> handler)
            => _logger.LogWarning(
                $"The message '{envelope.Message.Id}' couldn't be successfully " +
                $"processed and will be skipped.");
    }
}