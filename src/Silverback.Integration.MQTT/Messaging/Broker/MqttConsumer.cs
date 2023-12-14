// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Protocol;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <inheritdoc cref="Consumer{TIdentifier}" />
public class MqttConsumer : Consumer<MqttMessageIdentifier>
{
    private readonly IConsumerLogger<MqttConsumer> _logger;

    private readonly ConsumerChannelsManager _channelsManager;

    private readonly ConcurrentDictionary<string, ConsumedApplicationMessage> _inProcessingMessages = new();

    private readonly MqttConsumerEndpointsCache _endpointsCache;

    private bool _isDisposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttConsumer" /> class.
    /// </summary>
    /// <param name="name">
    ///     The consumer identifier.
    /// </param>
    /// <param name="client">
    ///     The <see cref="IMqttClientWrapper" />.
    /// </param>
    /// <param name="configuration">
    ///     The <see cref="MqttClientConfiguration" /> with only the consumer endpoints.
    /// </param>
    /// <param name="behaviorsProvider">
    ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="IConsumerLogger{TCategoryName}" />.
    /// </param>
    public MqttConsumer(
        string name,
        IMqttClientWrapper client,
        MqttClientConfiguration configuration,
        IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
        IServiceProvider serviceProvider,
        IConsumerLogger<MqttConsumer> logger)
        : base(
            name,
            client,
            Check.NotNull(configuration, nameof(configuration)).ConsumerEndpoints,
            behaviorsProvider,
            serviceProvider,
            logger)
    {
        Client = Check.NotNull(client, nameof(client));
        Configuration = Check.NotNull(configuration, nameof(configuration));
        _logger = Check.NotNull(logger, nameof(logger));

        _channelsManager = new ConsumerChannelsManager(this, logger);
        _endpointsCache = new MqttConsumerEndpointsCache(client.Configuration);

        Client.Connected.AddHandler(OnClientConnectedAsync);
        Client.Disconnected.AddHandler(OnClientDisconnectedAsync);
    }

    /// <inheritdoc cref="Consumer{TIdentifier}.Client" />
    public new IMqttClientWrapper Client { get; }

    /// <summary>
    ///     Gets the client configuration.
    /// </summary>
    public MqttClientConfiguration Configuration { get; }

    /// <inheritdoc cref="Consumer{TIdentifier}.EndpointsConfiguration" />
    public new IReadOnlyCollection<MqttConsumerEndpointConfiguration> EndpointsConfiguration => Configuration.ConsumerEndpoints;

    internal async Task HandleMessageAsync(ConsumedApplicationMessage message, ISequenceStore sequenceStore)
    {
        MessageHeaderCollection headers = Configuration.AreHeadersSupported
            ? new MessageHeaderCollection(message.ApplicationMessage.UserProperties?.ToSilverbackHeaders())
            : new MessageHeaderCollection();

        MqttConsumerEndpoint endpoint = _endpointsCache.GetEndpoint(message.ApplicationMessage.Topic);

        headers.AddIfNotExists(DefaultMessageHeaders.MessageId, message.Id);

        // If another message is still pending, cancel its task (might happen in case of timeout)
        // TODO: CHECK THIS!
        if (!_inProcessingMessages.TryAdd(message.Id, message))
            throw new InvalidOperationException("The message has been processed already.");

        await HandleMessageAsync(
                message.ApplicationMessage.Payload,
                headers,
                endpoint,
                new MqttMessageIdentifier(Configuration.ClientId, message.Id),
                sequenceStore)
            .ConfigureAwait(false);
    }

    /// <inheritdoc cref="Consumer{TIdentifier}.StartCoreAsync" />
    protected override ValueTask StartCoreAsync()
    {
        _channelsManager.StartReading();
        return default;
    }

    /// <inheritdoc cref="Consumer{TIdentifier}.StopCoreAsync" />
    protected override ValueTask StopCoreAsync()
    {
        _channelsManager.StopReadingAsync().FireAndForget();
        return default;
    }

    /// <inheritdoc cref="Consumer{TIdentifier}.WaitUntilConsumingStoppedCoreAsync" />
    protected override async ValueTask WaitUntilConsumingStoppedCoreAsync() => await _channelsManager.Stopping.ConfigureAwait(false);

    /// <inheritdoc cref="Consumer{TIdentifier}.CommitCoreAsync" />
    protected override async ValueTask CommitCoreAsync(IReadOnlyCollection<MqttMessageIdentifier> brokerMessageIdentifiers)
    {
        ConsumedApplicationMessage? consumedMessage = PullPendingConsumedMessage(brokerMessageIdentifiers);

        if (consumedMessage == null)
            return;

        await AcknowledgeAsync(consumedMessage).ConfigureAwait(false);
        consumedMessage.TaskCompletionSource.SetResult(true);
    }

    /// <inheritdoc cref="Consumer{TIdentifier}.RollbackCoreAsync" />
    protected override ValueTask RollbackCoreAsync(IReadOnlyCollection<MqttMessageIdentifier> brokerMessageIdentifiers)
    {
        ConsumedApplicationMessage? consumedMessage = PullPendingConsumedMessage(brokerMessageIdentifiers);
        consumedMessage?.TaskCompletionSource.SetResult(false);
        return default;
    }

    /// <inheritdoc cref="Consumer{TIdentifier}.Dispose(bool)" />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing || _isDisposed)
            return;

        _isDisposed = true;

        _channelsManager.Dispose();

        Client.Connected.RemoveHandler(OnClientConnectedAsync);
        Client.Disconnected.RemoveHandler(OnClientDisconnectedAsync);
    }

    private async ValueTask OnClientConnectedAsync(BrokerClient client)
    {
        Client.SubscribedTopicsFilters.ForEach(topicFilter => _logger.LogConsumerSubscribed(topicFilter.Topic, this));

        await StartAsync().ConfigureAwait(false);

        SetConnectedStatus();
    }

    private async ValueTask OnClientDisconnectedAsync(BrokerClient client)
    {
        await StopAsync().ConfigureAwait(false);
        RevertConnectedStatus();
    }

    private ConsumedApplicationMessage? PullPendingConsumedMessage(IReadOnlyCollection<MqttMessageIdentifier> brokerMessageIdentifiers)
    {
        Check.NotNull(brokerMessageIdentifiers, nameof(brokerMessageIdentifiers));

        string messageId = brokerMessageIdentifiers.Single().MessageId;
        _inProcessingMessages.TryRemove(messageId, out ConsumedApplicationMessage? message);
        return message;
    }

    private async Task AcknowledgeAsync(ConsumedApplicationMessage consumedMessage)
    {
        try
        {
            // TODO: Can pass a cancellation token?
            await consumedMessage.AcknowledgeAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Rethrow if the QoS level is exactly once, the consumer will be stopped
            if (consumedMessage.ApplicationMessage.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
                throw;

            _logger.LogAcknowledgeFailed(consumedMessage, this, ex);
        }
    }
}
