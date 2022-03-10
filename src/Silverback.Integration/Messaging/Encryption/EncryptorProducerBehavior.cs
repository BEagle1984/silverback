// Copyright (c) 2020 Sergio Aquilini
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
    public async Task HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        if (context.Envelope.Endpoint.Configuration.Encryption != null && context.Envelope.RawMessage != null)
        {
            context.Envelope.RawMessage = _streamFactory.GetEncryptStream(
                context.Envelope.RawMessage,
                context.Envelope.Endpoint.Configuration.Encryption);

            if (context.Envelope.Endpoint.Configuration.Encryption is SymmetricEncryptionSettings settings &&
                settings.KeyIdentifier != null)
            {
                context.Envelope.Headers.AddOrReplace(
                    DefaultMessageHeaders.EncryptionKeyId,
                    settings.KeyIdentifier);
            }
        }

        await next(context).ConfigureAwait(false);
    }
}
