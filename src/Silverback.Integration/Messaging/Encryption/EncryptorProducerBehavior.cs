// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Encryption
{
    public class EncryptorProducerBehavior : IProducerBehavior, ISorted
    {
        private readonly IMessageTransformerFactory _factory;

        public EncryptorProducerBehavior(IMessageTransformerFactory factory)
        {
            _factory = factory;
        }

        public async Task Handle(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            if (context.Envelope.Endpoint.Encryption != null)
            {
                context.Envelope.RawMessage = await _factory
                    .GetEncryptor(context.Envelope.Endpoint.Encryption)
                    .TransformAsync(context.Envelope.RawMessage, context.Envelope.Headers);
            }

            await next(context);
        }

        public int SortIndex => BrokerBehaviorsSortIndexes.Producer.Encryptor;
    }
}