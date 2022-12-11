// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
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

/// <inheritdoc cref="Producer{TEndpoint}" />
public sealed class MqttProducer : Producer<MqttProducerEndpoint>
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

    /// <inheritdoc cref="Producer{TEndpoint}.Client" />
    public new IMqttClientWrapper Client { get; }

    /// <summary>
    ///     Gets the producer configuration.
    /// </summary>
    public MqttClientConfiguration Configuration { get; }

    /// <inheritdoc cref="Producer{TEndpoint}.EndpointConfiguration" />
    public new MqttProducerEndpointConfiguration EndpointConfiguration { get; }

    /// <inheritdoc cref="Producer{TEndpoint}.ProduceCore(Stream,IReadOnlyCollection{MessageHeader},TEndpoint)" />
    protected override IBrokerMessageIdentifier? ProduceCore(
        Stream? message,
        IReadOnlyCollection<MessageHeader>? headers,
        MqttProducerEndpoint endpoint) =>
        AsyncHelper.RunSynchronously(() => ProduceCoreAsync(message, headers, endpoint));

    /// <inheritdoc cref="Producer{TEndpoint}.ProduceCore(byte[],IReadOnlyCollection{MessageHeader},TEndpoint)" />
    protected override IBrokerMessageIdentifier? ProduceCore(
        byte[]? message,
        IReadOnlyCollection<MessageHeader>? headers,
        MqttProducerEndpoint endpoint) =>
        AsyncHelper.RunSynchronously(() => ProduceCoreAsync(message, headers, endpoint));

    /// <inheritdoc cref="Producer{TEndpoint}.ProduceCore(Stream,IReadOnlyCollection{MessageHeader},TEndpoint,Action{IBrokerMessageIdentifier},Action{Exception})" />
    protected override void ProduceCore(
        Stream? message,
        IReadOnlyCollection<MessageHeader>? headers,
        MqttProducerEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        ProduceCore(message.ReadAll(), headers, endpoint, onSuccess, onError);

    /// <inheritdoc cref="Producer{TEndpoint}.ProduceCore(byte[],IReadOnlyCollection{MessageHeader},TEndpoint,Action{IBrokerMessageIdentifier},Action{Exception})" />
    protected override void ProduceCore(
        byte[]? message,
        IReadOnlyCollection<MessageHeader>? headers,
        MqttProducerEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError)
    {
        AsyncHelper.RunSynchronously(() => Client.ProduceAsync(message, headers, endpoint, onSuccess, onError));
    }

    /// <inheritdoc cref="Producer{TEndpoint}.ProduceCoreAsync(Stream,IReadOnlyCollection{MessageHeader},TEndpoint)" />
    protected override async ValueTask<IBrokerMessageIdentifier?> ProduceCoreAsync(
        Stream? message,
        IReadOnlyCollection<MessageHeader>? headers,
        MqttProducerEndpoint endpoint) =>
        await ProduceCoreAsync(await message.ReadAllAsync().ConfigureAwait(false), headers, endpoint).ConfigureAwait(false);

    /// <inheritdoc cref="Producer{TEndpoint}.ProduceCoreAsync(byte[],IReadOnlyCollection{MessageHeader},TEndpoint)" />
    protected override async ValueTask<IBrokerMessageIdentifier?> ProduceCoreAsync(
        byte[]? message,
        IReadOnlyCollection<MessageHeader>? headers,
        MqttProducerEndpoint endpoint)
    {
        TaskCompletionSource<IBrokerMessageIdentifier?> taskCompletionSource = new();

        await Client.ProduceAsync(
            message,
            headers,
            endpoint,
            identifier => taskCompletionSource.SetResult(identifier),
            exception => taskCompletionSource.SetException(exception)).ConfigureAwait(false);

        return await taskCompletionSource.Task.ConfigureAwait(false);
    }

    /// <inheritdoc cref="Producer{TEndpoint}.ProduceCoreAsync(Stream,IReadOnlyCollection{MessageHeader},TEndpoint,Action{IBrokerMessageIdentifier},Action{Exception})" />
    protected override async ValueTask ProduceCoreAsync(
        Stream? message,
        IReadOnlyCollection<MessageHeader>? headers,
        MqttProducerEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        await ProduceCoreAsync(
            await message.ReadAllAsync().ConfigureAwait(false),
            headers,
            endpoint,
            onSuccess,
            onError).ConfigureAwait(false);

    /// <inheritdoc cref="Producer{TEndpoint}.ProduceCoreAsync(byte[],IReadOnlyCollection{MessageHeader},TEndpoint,Action{IBrokerMessageIdentifier},Action{Exception})" />
    protected override ValueTask ProduceCoreAsync(
        byte[]? message,
        IReadOnlyCollection<MessageHeader>? headers,
        MqttProducerEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        Client.ProduceAsync(message, headers, endpoint, onSuccess, onError);
}
