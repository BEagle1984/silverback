// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Encryption;

/// <summary>
///     Decrypts the message according to the <see cref="IDecryptionSettings" />.
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
    public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        DecryptIfNeeded(context.Envelope);

        await next(context).ConfigureAwait(false);
    }

    private void DecryptIfNeeded(IRawInboundEnvelope envelope)
    {
        if (envelope.Endpoint.Configuration.Encryption == null || envelope.RawMessage == null)
            return;

        string? keyIdentifier = null;

        if (envelope.Endpoint.Configuration.Encryption is SymmetricDecryptionSettings { KeyProvider: not null })
            keyIdentifier = envelope.Headers.GetValue(DefaultMessageHeaders.EncryptionKeyId);

        envelope.RawMessage = _streamFactory.GetDecryptStream(envelope.RawMessage, envelope.Endpoint.Configuration.Encryption, keyIdentifier);
    }
}
