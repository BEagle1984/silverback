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
    ///     Decrypts the message according to the <see cref="EncryptionSettings" />.
    /// </summary>
    public class DecryptorConsumerBehavior : IConsumerBehavior
    {
        private readonly IMessageTransformerFactory _factory;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DecryptorConsumerBehavior" /> class.
        /// </summary>
        /// <param name="factory">
        ///     The <see cref="IMessageTransformerFactory" />.
        /// </param>
        public DecryptorConsumerBehavior(IMessageTransformerFactory factory)
        {
            _factory = factory;
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Decryptor;

        /// <inheritdoc cref="IConsumerBehavior.Handle" />
        public async Task Handle(
            ConsumerPipelineContext context,
            IServiceProvider serviceProvider,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(serviceProvider, nameof(serviceProvider));
            Check.NotNull(next, nameof(next));

            context.Envelopes = (await context.Envelopes.SelectAsync(Decrypt)).ToList();

            await next(context, serviceProvider);
        }

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
