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
            _logger = bus.GetLoggerFactory().CreateLogger<ErrorPolicyBase>();
            base.Init(bus);
        }
        /// <summary>
        /// Wraps the specified policy.
        /// </summary>
        /// <param name="policy">The policy to be executed if this one fails.</param>
        /// <returns></returns>
        public override IErrorPolicy Wrap(IErrorPolicy policy)
        {
            throw new NotSupportedException("This policy never fails and can't therefore wrap other policies.");
        }

        /// <summary>
        /// Applies the error handling policy.
        /// </summary>
        /// <param name="envelope">The envelope containing the failed message.</param>
        /// <param name="handler">The method that was used to handle the message.</param>
        protected override void ApplyPolicy(IEnvelope envelope, Action<IEnvelope> handler)
        {
            _logger.LogWarning($"The message '{envelope.Message.Id}' couldn't be successfully " +
                               $"processed and will be skipped.");
        }
    }
}