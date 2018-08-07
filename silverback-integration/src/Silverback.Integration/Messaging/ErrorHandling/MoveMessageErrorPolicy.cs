using System;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// This policy moves the failed messages to the configured endpoing (retry or bad mail queue).
    /// </summary>
    /// <seealso cref="Silverback.Messaging.ErrorHandling.ErrorPolicyBase" />
    public class MoveMessageErrorPolicy : ErrorPolicyBase
    {
        private readonly IEndpoint _endpoint;
        private IProducer _producer;

        /// <summary>
        /// Initializes the policy, binding to the specified bus.
        /// </summary>
        /// <param name="bus">The bus.</param>
        public override void Init(IBus bus)
        {
            _producer = bus.GetBroker(_endpoint.BrokerName).GetProducer(_endpoint);
            base.Init(bus);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveMessageErrorPolicy" /> class.
        /// </summary>
        /// <param name="endpoint">The target endpoint.</param>
        public MoveMessageErrorPolicy(IEndpoint endpoint)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        /// <summary>
        /// Applies the error handling policy.
        /// </summary>
        /// <param name="envelope">The envelope containing the failed message.</param>
        /// <param name="handler">The method that was used to handle the message.</param>
        /// <param name="exception">The exception that occurred.</param>
        protected override void ApplyPolicyImpl(IEnvelope envelope, Action<IEnvelope> handler, Exception exception)
            => _producer.Produce(envelope);
    }
}