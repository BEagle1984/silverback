// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dekaf.Errors;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <inheritdoc cref="Producer" />
public sealed class KafkaProducer : Producer
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaProducer" /> class.
    /// </summary>
    /// <param name="name">
    ///     The producer identifier.
    /// </param>
    /// <param name="client">
    ///     The <see cref="IDekafProducerWrapper" />.
    /// </param>
    /// <param name="configuration">
    ///     The configuration containing only the actual endpoint.
    /// </param>
    /// <param name="behaviorsProvider">
    ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger{TCategoryName}" />.
    /// </param>
    internal KafkaProducer(
        string name,
        IDekafProducerWrapper client,
        KafkaProducerConfiguration configuration,
        IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
        IServiceProvider serviceProvider,
        ISilverbackLogger<KafkaProducer> logger)
        : base(
            name,
            client,
            Check.NotNull(configuration, nameof(configuration)).Endpoints.Single(),
            behaviorsProvider,
            serviceProvider,
            logger)
    {
        Client = Check.NotNull(client, nameof(client));
        Configuration = Check.NotNull(configuration, nameof(configuration));

        EndpointConfiguration = Configuration.Endpoints.Single();

        Client.Producer = this;
    }

    /// <summary>
    ///     Gets the producer configuration.
    /// </summary>
    public KafkaProducerConfiguration Configuration { get; }

    /// <inheritdoc cref="Producer.EndpointConfiguration" />
    public new KafkaProducerEndpointConfiguration EndpointConfiguration { get; }

    /// <summary>
    ///     Gets the wrapper around the underlying Dekaf producer.
    /// </summary>
    internal new IDekafProducerWrapper Client { get; }

    /// <inheritdoc cref="Producer.ProduceCore(IOutboundEnvelope)" />
    protected override IBrokerMessageIdentifier? ProduceCore(IOutboundEnvelope envelope) =>
        ProduceCoreAsync(envelope, CancellationToken.None).SafeWait();

    /// <inheritdoc cref="Producer.ProduceCore{TState}(IOutboundEnvelope,Action{IBrokerMessageIdentifier,TState},Action{Exception,TState},TState)" />
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception forwarded")]
    protected override void ProduceCore<TState>(
        IOutboundEnvelope envelope,
        Action<IBrokerMessageIdentifier, TState> onSuccess,
        Action<Exception, TState> onError,
        TState state)
    {
        Check.NotNull(envelope, nameof(envelope));
        Check.NotNull(onSuccess, nameof(onSuccess));
        Check.NotNull(onError, nameof(onError));

        KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)envelope.GetEndpoint();

        Client.Produce(
            endpoint.TopicPartition,
            GetKafkaKey(envelope),
            envelope.RawMessage.ReadAll(),
            envelope.Headers,
            topicPartitionOffset =>
            {
                try
                {
                    onSuccess.Invoke(new KafkaOffset(topicPartitionOffset), state);
                }
                catch (Exception ex)
                {
                    onError.Invoke(ex, state);
                }
            },
            exception => onError.Invoke(new ProduceException("Error occurred producing the message. See inner exception for details.", exception), state));
    }

    /// <inheritdoc cref="Producer.ProduceCoreAsync(IOutboundEnvelope,CancellationToken)" />
    protected override async ValueTask<IBrokerMessageIdentifier?> ProduceCoreAsync(
        IOutboundEnvelope envelope,
        CancellationToken cancellationToken)
    {
        Check.NotNull(envelope, nameof(envelope));

        try
        {
            KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)envelope.GetEndpoint();

            TopicPartitionOffset topicPartitionOffset = await Client.ProduceAsync(
                    endpoint.TopicPartition,
                    GetKafkaKey(envelope),
                    await envelope.RawMessage.ReadAllAsync().ConfigureAwait(false),
                    envelope.Headers,
                    cancellationToken)
                .ConfigureAwait(false);

            return new KafkaOffset(topicPartitionOffset);
        }
        catch (KafkaException ex)
        {
            throw new ProduceException("Error occurred producing the message. See inner exception for details.", ex);
        }
    }

    private static byte[]? GetKafkaKey(IOutboundEnvelope envelope) =>
        envelope.Headers.TryGetValue(KafkaMessageHeaders.MessageKey, out string? kafkaKey) && kafkaKey != null
            ? Encoding.UTF8.GetBytes(kafkaKey)
            : null;
}
