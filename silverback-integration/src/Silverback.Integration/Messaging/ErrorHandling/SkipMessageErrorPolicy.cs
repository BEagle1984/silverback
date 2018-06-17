using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// This policy simply skips the message that failed to be processed.
    /// </summary>
    /// <seealso cref="Silverback.Messaging.ErrorHandling.ErrorPolicyBase" />
    public class SkipMessageErrorPolicy : ErrorPolicyBase
    {
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
            // TODO: Trace
        }
    }
}