﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <inheritdoc cref="Consumer{TBroker,TEndpoint, TIdentifier}" />
public class MqttConsumer : Consumer<MqttBroker, MqttConsumerConfiguration, MqttMessageIdentifier>
{
    [SuppressMessage("", "CA2213", Justification = "Disposed by the MqttClientCache")]
    private readonly MqttClientWrapper _clientWrapper;

    private readonly ConsumerChannelManager _channelManager;

    private readonly ConcurrentDictionary<string, ConsumedApplicationMessage> _inProcessingMessages = new();

    private readonly Dictionary<string, MqttConsumerEndpoint> _endpointsCache = new();

    private readonly Func<string, MqttConsumerEndpoint> _endpointFactory;

    private bool _disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttConsumer" /> class.
    /// </summary>
    /// <param name="broker">
    ///     The <see cref="IBroker" /> that is instantiating the consumer.
    /// </param>
    /// <param name="configuration">
    ///     The <see cref="MqttConsumerConfiguration" />.
    /// </param>
    /// <param name="behaviorsProvider">
    ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="IInboundLogger{TCategoryName}" />.
    /// </param>
    public MqttConsumer(
        MqttBroker broker,
        MqttConsumerConfiguration configuration,
        IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
        IServiceProvider serviceProvider,
        IInboundLogger<MqttConsumer> logger)
        : base(broker, configuration, behaviorsProvider, serviceProvider, logger)
    {
        Check.NotNull(serviceProvider, nameof(serviceProvider));
        Check.NotNull(logger, nameof(logger));

        _clientWrapper = serviceProvider
            .GetRequiredService<IMqttClientsCache>()
            .GetClient(this);

        _channelManager = new ConsumerChannelManager(_clientWrapper, logger);

        _endpointFactory = topic => new MqttConsumerEndpoint(topic, configuration);
    }

    internal async Task HandleMessageAsync(ConsumedApplicationMessage message)
    {
        MessageHeaderCollection headers = Configuration.Client.AreHeadersSupported
            ? new MessageHeaderCollection(message.ApplicationMessage.UserProperties.ToSilverbackHeaders())
            : new MessageHeaderCollection();

        MqttConsumerEndpoint actualEndpoint = _endpointsCache.GetOrAdd(message.ApplicationMessage.Topic, _endpointFactory);

        headers.AddIfNotExists(DefaultMessageHeaders.MessageId, message.Id);

        // If another message is still pending, cancel it's task (might happen in case of timeout)
        if (!_inProcessingMessages.TryAdd(message.Id, message))
            throw new InvalidOperationException("The message has been processed already.");

        await HandleMessageAsync(
                message.ApplicationMessage.Payload,
                headers,
                actualEndpoint,
                new MqttMessageIdentifier(Configuration.Client.ClientId, message.Id))
            .ConfigureAwait(false);
    }

    internal async Task OnConnectionEstablishedAsync()
    {
        await _clientWrapper.SubscribeAsync(
                Configuration.Topics.Select(
                        topic =>
                            new MqttTopicFilterBuilder()
                                .WithTopic(topic)
                                .WithQualityOfServiceLevel(Configuration.QualityOfServiceLevel)
                                .Build())
                    .ToArray())
            .ConfigureAwait(false);

        if (IsConnected)
            await StartAsync().ConfigureAwait(false);

        SetReadyStatus();
    }

    internal async Task OnConnectionLostAsync()
    {
        await StopAsync().ConfigureAwait(false);

        RevertReadyStatus();

        await WaitUntilConsumingStoppedCoreAsync().ConfigureAwait(false);
    }

    /// <inheritdoc cref="Consumer{TBroker,TEndpoint, TIdentifier}.ConnectCoreAsync" />
    protected override Task ConnectCoreAsync() => _clientWrapper.ConnectAsync(this);

    /// <inheritdoc cref="Consumer{TBroker,TEndpoint, TIdentifier}.DisconnectCoreAsync" />
    protected override async Task DisconnectCoreAsync()
    {
        await _clientWrapper.UnsubscribeAsync(Configuration.Topics).ConfigureAwait(false);
        await _clientWrapper.DisconnectAsync(this).ConfigureAwait(false);
    }

    /// <inheritdoc cref="Consumer{TBroker,TEndpoint, TIdentifier}.StartCoreAsync" />
    protected override Task StartCoreAsync()
    {
        if (_clientWrapper == null)
            throw new InvalidOperationException("The consumer is not connected.");

        _channelManager.StartReading();
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="Consumer{TBroker,TEndpoint, TIdentifier}.StopCoreAsync" />
    protected override Task StopCoreAsync()
    {
        _channelManager.StopReading();
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="Consumer{TBroker,TEndpoint, TIdentifier}.WaitUntilConsumingStoppedCoreAsync" />
    protected override Task WaitUntilConsumingStoppedCoreAsync() => _channelManager.Stopping;

    /// <inheritdoc cref="Consumer{TBroker,TEndpoint, TIdentifier}.CommitCoreAsync" />
    protected override Task CommitCoreAsync(IReadOnlyCollection<MqttMessageIdentifier> brokerMessageIdentifiers) =>
        SetProcessingCompletedAsync(brokerMessageIdentifiers, true);

    /// <inheritdoc cref="Consumer{TBroker,TEndpoint, TIdentifier}.RollbackCoreAsync" />
    protected override Task RollbackCoreAsync(IReadOnlyCollection<MqttMessageIdentifier> brokerMessageIdentifiers) =>
        SetProcessingCompletedAsync(brokerMessageIdentifiers, false);

    /// <inheritdoc cref="Consumer{TBroker,TEndpoint, TIdentifier}.Dispose(bool)" />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing || _disposed)
            return;

        _disposed = true;

        _channelManager.Dispose();
    }

    private Task SetProcessingCompletedAsync(
        IReadOnlyCollection<MqttMessageIdentifier> brokerMessageIdentifiers,
        bool isSuccess)
    {
        Check.NotNull(brokerMessageIdentifiers, nameof(brokerMessageIdentifiers));

        string messageId = brokerMessageIdentifiers.Single().MessageId;

        if (!_inProcessingMessages.TryRemove(messageId, out ConsumedApplicationMessage? message))
            return Task.CompletedTask;

        message.TaskCompletionSource.SetResult(isSuccess);
        return Task.CompletedTask;
    }
}
