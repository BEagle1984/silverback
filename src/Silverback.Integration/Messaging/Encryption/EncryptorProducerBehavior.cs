// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Encryption;

/// <summary>
///     Encrypts the message according to the <see cref="IEncryptionSettings" />.
/// </summary>
public class EncryptorProducerBehavior : IProducerBehavior
{
    private readonly ISilverbackCryptoStreamFactory _streamFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EncryptorProducerBehavior" /> class.
    /// </summary>
    /// <param name="streamFactory">
    ///     The <see cref="ISilverbackCryptoStreamFactory" />.
    /// </param>
    public EncryptorProducerBehavior(ISilverbackCryptoStreamFactory streamFactory)
    {
        _streamFactory = streamFactory;
    }

    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Producer.Encryptor;

    /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
    public async ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        EncryptIfNeeded(context.Envelope);

        await next(context).ConfigureAwait(false);
    }

    private void EncryptIfNeeded(IRawOutboundEnvelope envelope)
    {
        if (envelope.Endpoint.Configuration.Encryption == null || envelope.RawMessage == null)
            return;

        envelope.RawMessage = _streamFactory.GetEncryptStream(envelope.RawMessage, envelope.Endpoint.Configuration.Encryption);

        if (envelope.Endpoint.Configuration.Encryption is SymmetricEncryptionSettings { KeyIdentifier: { } } settings)
            envelope.Headers.AddOrReplace(DefaultMessageHeaders.EncryptionKeyId, settings.KeyIdentifier);
    }
}
