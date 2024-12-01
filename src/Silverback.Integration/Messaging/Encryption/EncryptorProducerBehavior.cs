// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
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
    public async ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        EncryptIfNeeded(context.Envelope);

        await next(context, cancellationToken).ConfigureAwait(false);
    }

    private void EncryptIfNeeded(IOutboundEnvelope envelope)
    {
        if (envelope.EndpointConfiguration.Encryption == null || envelope.RawMessage == null)
            return;

        envelope.RawMessage = _streamFactory.GetEncryptStream(envelope.RawMessage, envelope.EndpointConfiguration.Encryption);

        if (envelope.EndpointConfiguration.Encryption is SymmetricEncryptionSettings { KeyIdentifier: not null } settings)
            envelope.Headers.AddOrReplace(DefaultMessageHeaders.EncryptionKeyId, settings.KeyIdentifier);
    }
}
