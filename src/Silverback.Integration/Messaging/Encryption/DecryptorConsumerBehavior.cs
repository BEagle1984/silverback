// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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
        private readonly ISilverbackCryptoStreamFactory _streamFactory;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DecryptorConsumerBehavior" /> class.
        /// </summary>
        /// <param name="streamFactory">
        ///     The <see cref="ISilverbackCryptoStreamFactory" />.
        /// </param>
        public DecryptorConsumerBehavior(ISilverbackCryptoStreamFactory streamFactory)
        {
            _streamFactory = streamFactory;
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Decryptor;

        /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
        public async Task HandleAsync(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            if (context.Envelope.Endpoint.Encryption != null &&
                !context.Envelope.Headers.Contains(DefaultMessageHeaders.Decrypted) &&
                context.Envelope.RawMessage != null)
            {
                context.Envelope.RawMessage = _streamFactory.GetDecryptStream(
                    context.Envelope.RawMessage,
                    context.Envelope.Endpoint.Encryption);
            }

            await next(context).ConfigureAwait(false);
        }
    }
}
