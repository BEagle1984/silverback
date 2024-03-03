// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <inheritdoc cref="Producer" />
public sealed class MqttProducer : Producer
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttProducer" /> class.
    /// </summary>
    /// <param name="name">
    ///     The producer identifier.
    /// </param>
    /// <param name="client">
    ///     The <see cref="IMqttClientWrapper" />.
    /// </param>
    /// <param name="configuration">
    ///     The configuration containing only the actual endpoint.
    /// </param>
    /// <param name="behaviorsProvider">
    ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
    /// </param>
    /// <param name="envelopeFactory">
    ///     The <see cref="IOutboundEnvelopeFactory" />.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="IProducerLogger{TCategoryName}" />.
    /// </param>
    public MqttProducer(
        string name,
        IMqttClientWrapper client,
        MqttClientConfiguration configuration,
        IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
        IOutboundEnvelopeFactory envelopeFactory,
        IServiceProvider serviceProvider,
        IProducerLogger<MqttProducer> logger)
        : base(
            name,
            client,
            Check.NotNull(configuration, nameof(configuration)).ProducerEndpoints.Single(),
            behaviorsProvider,
            envelopeFactory,
            serviceProvider,
            logger)
    {
        Client = Check.NotNull(client, nameof(client));
        Configuration = Check.NotNull(configuration, nameof(configuration));

        EndpointConfiguration = Configuration.ProducerEndpoints.Single();
    }

    /// <inheritdoc cref="Producer.Client" />
    public new IMqttClientWrapper Client { get; }

    /// <summary>
    ///     Gets the producer configuration.
    /// </summary>
    public MqttClientConfiguration Configuration { get; }

    /// <inheritdoc cref="Producer.EndpointConfiguration" />
    public new MqttProducerEndpointConfiguration EndpointConfiguration { get; }

    /// <inheritdoc cref="Producer.ProduceCore(IOutboundEnvelope)" />
    protected override IBrokerMessageIdentifier? ProduceCore(IOutboundEnvelope envelope) =>
        AsyncHelper.RunSynchronously(() => ProduceCoreAsync(envelope));

    /// <inheritdoc cref="Producer.ProduceCore(IOutboundEnvelope,Action{IBrokerMessageIdentifier},Action{Exception})" />
    protected override void ProduceCore(
        IOutboundEnvelope envelope,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError)
    {
        Check.NotNull(envelope, nameof(envelope));

        Client.Produce(
            envelope.RawMessage.ReadAll(),
            envelope.Headers,
            (MqttProducerEndpoint)envelope.Endpoint,
            onSuccess,
            onError);
    }

    /// <inheritdoc cref="Producer.ProduceCoreAsync(IOutboundEnvelope)" />
    protected override async ValueTask<IBrokerMessageIdentifier?> ProduceCoreAsync(IOutboundEnvelope envelope)
    {
        Check.NotNull(envelope, nameof(envelope));

        TaskCompletionSource<IBrokerMessageIdentifier?> taskCompletionSource = new();

        Client.Produce(
            await envelope.RawMessage.ReadAllAsync().ConfigureAwait(true),
            envelope.Headers,
            (MqttProducerEndpoint)envelope.Endpoint,
            taskCompletionSource.SetResult,
            taskCompletionSource.SetException);

        return await taskCompletionSource.Task.ConfigureAwait(false);
    }
}
