// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Protocol;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Consumer{TBroker,TEndpoint, TIdentifier}" />
    public class MqttConsumer : Consumer<MqttBroker, MqttConsumerEndpoint, MqttMessageIdentifier>
    {
        [SuppressMessage("", "CA2213", Justification = "Disposed by the MqttClientCache")]
        private readonly MqttClientWrapper _clientWrapper;

        private readonly ConcurrentDictionary<string, ConsumedApplicationMessage> _inProcessingMessages =
            new();

        private readonly IInboundLogger<MqttConsumer> _logger;

        private ConsumerChannelsManager? _channelsManager;

        private bool _disposed;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MqttConsumer" /> class.
        /// </summary>
        /// <param name="broker">
        ///     The <see cref="IBroker" /> that is instantiating the consumer.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint to be consumed.
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
            MqttConsumerEndpoint endpoint,
            IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider,
            IInboundLogger<MqttConsumer> logger)
            : base(broker, endpoint, behaviorsProvider, serviceProvider, logger)
        {
            Check.NotNull(serviceProvider, nameof(serviceProvider));
            _logger = Check.NotNull(logger, nameof(logger));

            _clientWrapper = serviceProvider
                .GetRequiredService<IMqttClientsCache>()
                .GetClient(this);
        }

        /// <inheritdoc cref="Consumer.GetCurrentSequenceStores" />
        public override IReadOnlyList<ISequenceStore> GetCurrentSequenceStores() =>
            _channelsManager?.SequenceStores ?? Array.Empty<ISequenceStore>();

        internal async Task HandleMessageAsync(
            ConsumedApplicationMessage message,
            ISequenceStore sequenceStore)
        {
            var headers = Endpoint.Configuration.AreHeadersSupported
                ? new MessageHeaderCollection(message.EventArgs.ApplicationMessage.UserProperties?.ToSilverbackHeaders())
                : new MessageHeaderCollection();

            headers.AddIfNotExists(DefaultMessageHeaders.MessageId, message.Id);

            // If the same message is still pending, wait for the processing task to complete
            // (avoid issues when retrying the same message by moving to the same topic)
            if (!_inProcessingMessages.TryAdd(message.Id, message))
            {
                if (_inProcessingMessages.TryGetValue(message.Id, out var inProcessingMessage))
                    await inProcessingMessage.TaskCompletionSource.Task.ConfigureAwait(false);

                if (!_inProcessingMessages.TryAdd(message.Id, message))
                    throw new InvalidOperationException("The message has been processed already.");
            }

            await HandleMessageAsync(
                    message.EventArgs.ApplicationMessage.PayloadSegment.ToArray(),
                    headers,
                    message.EventArgs.ApplicationMessage.Topic,
                    new MqttMessageIdentifier(Endpoint.Configuration.ClientId, message.Id),
                    sequenceStore)
                .ConfigureAwait(false);
        }

        internal async Task OnConnectionEstablishedAsync()
        {
            await _clientWrapper.SubscribeAsync(
                    Endpoint.Topics.Select(
                            topic =>
                                new MqttTopicFilterBuilder()
                                    .WithTopic(topic)
                                    .WithQualityOfServiceLevel(Endpoint.QualityOfServiceLevel)
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

        /// <inheritdoc cref="Consumer.ConnectCoreAsync" />
        protected override Task ConnectCoreAsync()
        {
            _channelsManager = new ConsumerChannelsManager(
                _clientWrapper,
                Check.NotNull(Endpoint, nameof(Endpoint)),
                () => ServiceProvider.GetRequiredService<ISequenceStore>(),
                _logger);

            return _clientWrapper.ConnectAsync(this);
        }

        /// <inheritdoc cref="Consumer.DisconnectCoreAsync" />
        protected override Task DisconnectCoreAsync()
        {
            _channelsManager?.Dispose();
            _channelsManager = null;

            return _clientWrapper.DisconnectAsync(this);
        }

        /// <inheritdoc cref="Consumer.StartCoreAsync" />
        protected override Task StartCoreAsync()
        {
            if (_clientWrapper == null || _channelsManager == null)
                throw new InvalidOperationException("The consumer is not connected.");

            _channelsManager.StartReading();
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer.StopCoreAsync" />
        protected override Task StopCoreAsync()
        {
            _channelsManager?.StopReadingAsync().FireAndForget();
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer.WaitUntilConsumingStoppedCoreAsync" />
        protected override Task WaitUntilConsumingStoppedCoreAsync() =>
            _channelsManager?.Stopping ?? Task.CompletedTask;

        /// <inheritdoc cref="Consumer.CommitCoreAsync" />
        protected override async Task CommitCoreAsync(IReadOnlyCollection<MqttMessageIdentifier> brokerMessageIdentifiers)
        {
            var consumedMessage = PullPendingConsumedMessage(brokerMessageIdentifiers);

            if (consumedMessage == null)
                return;

            await AcknowledgeAsync(consumedMessage).ConfigureAwait(false);
            consumedMessage.TaskCompletionSource.SetResult(true);
        }

        /// <inheritdoc cref="Consumer.RollbackCoreAsync" />
        protected override Task RollbackCoreAsync(IReadOnlyCollection<MqttMessageIdentifier> brokerMessageIdentifiers)
        {
            var consumedMessage = PullPendingConsumedMessage(brokerMessageIdentifiers);
            consumedMessage?.TaskCompletionSource.SetResult(false);
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer.Dispose(bool)" />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing || _disposed)
                return;

            _disposed = true;

            _channelsManager?.Dispose();
            _channelsManager = null;
        }

        private ConsumedApplicationMessage? PullPendingConsumedMessage(IReadOnlyCollection<MqttMessageIdentifier> brokerMessageIdentifiers)
        {
            Check.NotNull(brokerMessageIdentifiers, nameof(brokerMessageIdentifiers));

            string messageId = brokerMessageIdentifiers.Single().MessageId;
            _inProcessingMessages.TryRemove(messageId, out var message);
            return message;
        }

        private async Task AcknowledgeAsync(ConsumedApplicationMessage consumedMessage)
        {
            try
            {
                await consumedMessage.EventArgs.AcknowledgeAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Rethrow if the QoS level is exactly once, the consumer will be stopped
                if (consumedMessage.EventArgs.ApplicationMessage.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
                    throw;

                _logger.LogAcknowledgeFailed(consumedMessage, this, ex);
            }
        }
    }
}
