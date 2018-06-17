using System;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// This policy moves the failed messages to the configured endpoing (retry or bad mail queue).
    /// </summary>
    /// <seealso cref="Silverback.Messaging.ErrorHandling.ErrorPolicyBase" />
    public class MoveMessageErrorPolicy : ErrorPolicyBase
    {
        private readonly IProducer _producer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveMessageErrorPolicy" /> class.
        /// </summary>
        /// <param name="broker">The broker.</param>
        /// <param name="endpoint">The target endpoint.</param>
        public MoveMessageErrorPolicy(IBroker broker, IEndpoint endpoint)
        {
            if (broker == null) throw new ArgumentNullException(nameof(broker));
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
            _producer = broker.GetProducer(endpoint);
        }

        /// <summary>
        /// Applies the error handling policy.
        /// </summary>
        /// <param name="envelope">The envelope containing the failed message.</param>
        /// <param name="handler">The method that was used to handle the message.</param>
        protected override void ApplyPolicy(IEnvelope envelope, Action<IEnvelope> handler)
            => _producer.Produce(envelope);
    }
}