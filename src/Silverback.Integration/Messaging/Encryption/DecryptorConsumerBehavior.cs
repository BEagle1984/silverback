// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Encryption
{
    /// <summary>
    ///     Decrypts the message according to the <see cref="EncryptionSettings"/>.
    /// </summary>
    public class DecryptorConsumerBehavior : IConsumerBehavior, ISorted
    {
        private readonly IMessageTransformerFactory _factory;

        public DecryptorConsumerBehavior(IMessageTransformerFactory factory)
        {
            _factory = factory;
        }

        public async Task Handle(ConsumerPipelineContext context, IServiceProvider serviceProvider, ConsumerBehaviorHandler next)
        {
            context.Envelopes = (await context.Envelopes.SelectAsync(Decrypt)).ToList();

            await next(context, serviceProvider);
        }

        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Decryptor;

        private async Task<IRawInboundEnvelope> Decrypt(IRawInboundEnvelope envelope)
        {
            if (envelope.Endpoint.Encryption != null && 
                !envelope.Headers.Contains(DefaultMessageHeaders.Decrypted))
            {
                envelope.RawMessage = await _factory
                    .GetDecryptor(envelope.Endpoint.Encryption)
                    .TransformAsync(envelope.RawMessage, envelope.Headers);
                
                envelope.Headers.Add(DefaultMessageHeaders.Decrypted, true);
            }

            return envelope;
        }
    }
}