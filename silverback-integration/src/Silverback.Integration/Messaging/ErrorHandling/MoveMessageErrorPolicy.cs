using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// This policy moves the failed messages to the configured endpoing (retry or bad mail queue).
    /// </summary>
    public class MoveMessageErrorPolicy : ErrorPolicyBase
    {
        private readonly IProducer _producer;

        public MoveMessageErrorPolicy(IBroker broker, IEndpoint endpoint, ILogger<MoveMessageErrorPolicy> logger) 
            : base(logger)
        {
            _producer = broker.GetProducer(endpoint);
        }

        // TODO: Should be async? All way to ErrorPolicyBase.TryHandleMessage() or nothing...
        protected override void ApplyPolicyImpl(IEnvelope envelope, Action<IEnvelope> handler, Exception exception)
            => _producer.Produce(envelope);
    }
}