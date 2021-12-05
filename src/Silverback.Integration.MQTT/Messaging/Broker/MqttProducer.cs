// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Client.Publishing;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}" />
public sealed class MqttProducer : Producer<MqttBroker, MqttProducerConfiguration, MqttProducerEndpoint>, IDisposable
{
    [SuppressMessage("", "CA2213", Justification = "Disposed by the MqttClientCache")]
    private readonly MqttClientWrapper _clientWrapper;

    private readonly ISilverbackLogger _logger;

    private readonly Channel<QueuedMessage> _queueChannel = Channel.CreateUnbounded<QueuedMessage>();

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttProducer" /> class.
    /// </summary>
    /// <param name="broker">
    ///     The <see cref="IBroker" /> that instantiated this producer.
    /// </param>
    /// <param name="configuration">
    ///     The <see cref="MqttProducerConfiguration" />.
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
    ///     The <see cref="IOutboundLogger{TCategoryName}" />.
    /// </param>
    public MqttProducer(
        MqttBroker broker,
        MqttProducerConfiguration configuration,
        IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
        IOutboundEnvelopeFactory envelopeFactory,
        IServiceProvider serviceProvider,
        IOutboundLogger<MqttProducer> logger)
        : base(broker, configuration, behaviorsProvider, envelopeFactory, serviceProvider, logger)
    {
        Check.NotNull(serviceProvider, nameof(serviceProvider));
        _clientWrapper = serviceProvider
            .GetRequiredService<IMqttClientsCache>()
            .GetClient(this);
        _logger = Check.NotNull(logger, nameof(logger));

        Task.Run(() => ProcessQueueAsync(_cancellationTokenSource.Token)).FireAndForget();
    }

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose()
    {
        Flush();

        if (!_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }

    /// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}.ConnectCoreAsync" />
    protected override Task ConnectCoreAsync() =>
        _clientWrapper.ConnectAsync(this);

    /// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}.DisconnectCoreAsync" />
    protected override Task DisconnectCoreAsync() =>
        _clientWrapper.DisconnectAsync(this);

    /// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}.ProduceCore(object,Stream,IReadOnlyCollection{MessageHeader},TEndpoint)" />
    protected override IBrokerMessageIdentifier? ProduceCore(
        object? message,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        MqttProducerEndpoint endpoint) =>
        AsyncHelper.RunSynchronously(() => ProduceCoreAsync(message, messageStream, headers, endpoint));

    /// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}.ProduceCore(object,byte[],IReadOnlyCollection{MessageHeader},TEndpoint)" />
    protected override IBrokerMessageIdentifier? ProduceCore(
        object? message,
        byte[]? messageBytes,
        IReadOnlyCollection<MessageHeader>? headers,
        MqttProducerEndpoint endpoint) =>
        AsyncHelper.RunSynchronously(() => ProduceCoreAsync(message, messageBytes, headers, endpoint));

    /// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}.ProduceCore(object,Stream,IReadOnlyCollection{MessageHeader},TEndpoint,Action{IBrokerMessageIdentifier},Action{Exception})" />
    protected override void ProduceCore(
        object? message,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        MqttProducerEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        ProduceCore(message, messageStream.ReadAll(), headers, endpoint, onSuccess, onError);

    /// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}.ProduceCore(object,byte[],IReadOnlyCollection{MessageHeader},TEndpoint,Action{IBrokerMessageIdentifier},Action{Exception})" />
    protected override void ProduceCore(
        object? message,
        byte[]? messageBytes,
        IReadOnlyCollection<MessageHeader>? headers,
        MqttProducerEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError)
    {
        QueuedMessage queuedMessage = new(messageBytes, headers, endpoint, onSuccess, onError);
        AsyncHelper.RunSynchronously(() => _queueChannel.Writer.WriteAsync(queuedMessage).AsTask());
    }

    /// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}.ProduceCoreAsync(object,Stream,IReadOnlyCollection{MessageHeader},TEndpoint)" />
    protected override async Task<IBrokerMessageIdentifier?> ProduceCoreAsync(
        object? message,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        MqttProducerEndpoint endpoint) =>
        await ProduceCoreAsync(message, await messageStream.ReadAllAsync().ConfigureAwait(false), headers, endpoint).ConfigureAwait(false);

    /// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}.ProduceCoreAsync(object,byte[],IReadOnlyCollection{MessageHeader},TEndpoint)" />
    protected override async Task<IBrokerMessageIdentifier?> ProduceCoreAsync(
        object? message,
        byte[]? messageBytes,
        IReadOnlyCollection<MessageHeader>? headers,
        MqttProducerEndpoint endpoint)
    {
        QueuedMessage queuedMessage = new(messageBytes, headers, endpoint, null, null);

        await _queueChannel.Writer.WriteAsync(queuedMessage).ConfigureAwait(false);
        await queuedMessage.TaskCompletionSource.Task.ConfigureAwait(false);

        return null;
    }

    /// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}.ProduceCoreAsync(object,Stream,IReadOnlyCollection{MessageHeader},TEndpoint,Action{IBrokerMessageIdentifier},Action{Exception})" />
    protected override async Task ProduceCoreAsync(
        object? message,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        MqttProducerEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        await ProduceCoreAsync(
            message,
            await messageStream.ReadAllAsync().ConfigureAwait(false),
            headers,
            endpoint,
            onSuccess,
            onError).ConfigureAwait(false);

    /// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}.ProduceCoreAsync(object,byte[],IReadOnlyCollection{MessageHeader},TEndpoint,Action{IBrokerMessageIdentifier},Action{Exception})" />
    protected override async Task ProduceCoreAsync(
        object? message,
        byte[]? messageBytes,
        IReadOnlyCollection<MessageHeader>? headers,
        MqttProducerEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError)
    {
        QueuedMessage queuedMessage = new(messageBytes, headers, endpoint, onSuccess, onError);
        await _queueChannel.Writer.WriteAsync(queuedMessage).ConfigureAwait(false);
    }

    [SuppressMessage("", "CA1031", Justification = "Exception logged/forwarded")]
    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                QueuedMessage queuedMessage = await _queueChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    await PublishToTopicAsync(queuedMessage, cancellationToken).ConfigureAwait(false);

                    queuedMessage.OnSuccess?.Invoke(null);
                    queuedMessage.TaskCompletionSource.SetResult(null);
                }
                catch (Exception ex)
                {
                    ProduceException produceException =
                        new("Error occurred producing the message. See inner exception for details.", ex);

                    queuedMessage.OnError?.Invoke(produceException);
                    queuedMessage.TaskCompletionSource.SetException(produceException);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogProducerQueueProcessingCanceled(this);
        }
    }

    private async Task PublishToTopicAsync(
        QueuedMessage queuedMessage,
        CancellationToken cancellationToken)
    {
        MqttApplicationMessage mqttApplicationMessage = new()
        {
            Topic = queuedMessage.Endpoint.Topic,
            Payload = queuedMessage.MessageBytes,
            QualityOfServiceLevel = Configuration.QualityOfServiceLevel,
            Retain = Configuration.Retain,
            MessageExpiryInterval = Configuration.MessageExpiryInterval
        };

        if (queuedMessage.Headers != null && Configuration.Client.AreHeadersSupported)
            mqttApplicationMessage.UserProperties = queuedMessage.Headers.ToUserProperties();

        while (!_clientWrapper.MqttClient.IsConnected)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        }

        MqttClientPublishResult? result = await _clientWrapper.MqttClient.PublishAsync(
                mqttApplicationMessage,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.ReasonCode != MqttClientPublishReasonCode.Success)
        {
            throw new MqttProduceException(
                "Error occurred producing the message to the MQTT broker. See the Result property for details.",
                result);
        }
    }

    private void Flush()
    {
        if (_queueChannel.Reader.Completion.IsCompleted)
            return;

        _queueChannel.Writer.Complete();
        _queueChannel.Reader.Completion.Wait();
    }

    private sealed record QueuedMessage(
        byte[]? MessageBytes,
        IReadOnlyCollection<MessageHeader>? Headers,
        MqttProducerEndpoint Endpoint,
        Action<IBrokerMessageIdentifier?>? OnSuccess,
        Action<Exception>? OnError)
    {
        public TaskCompletionSource<IBrokerMessageIdentifier?> TaskCompletionSource { get; } = new();
    }
}
