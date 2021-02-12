// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Consumer{TBroker,TEndpoint, TIdentifier}" />
    public class MqttConsumer : Consumer<MqttBroker, MqttConsumerEndpoint, MqttMessageIdentifier>
    {
        private readonly IInboundLogger<MqttConsumer> _logger;

        private readonly IMqttClientsCache _clientFactory;

        private ConsumerChannelManager? _channelManager;

        private MqttClientWrapper? _clientWrapper;

        private ConsumedApplicationMessage? _inProcessingMessage;

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

            _clientFactory = serviceProvider.GetRequiredService<IMqttClientsCache>();
        }

        internal async Task HandleMessageAsync(ConsumedApplicationMessage message)
        {
            var headers = Endpoint.Configuration.AreHeadersSupported
                ? new MessageHeaderCollection(message.ApplicationMessage.UserProperties.ToSilverbackHeaders())
                : new MessageHeaderCollection();

            headers.AddIfNotExists(DefaultMessageHeaders.MessageId, message.Id);

            _inProcessingMessage = message;

            await HandleMessageAsync(
                    message.ApplicationMessage.Payload,
                    headers,
                    message.ApplicationMessage.Topic,
                    new MqttMessageIdentifier(Endpoint.Configuration.ClientId, message.Id))
                .ConfigureAwait(false);
        }

        /// <inheritdoc cref="Consumer.ConnectCoreAsync" />
        protected override async Task ConnectCoreAsync()
        {
            _clientWrapper = _clientFactory.GetClient(this);

            await _clientWrapper.ConnectAsync(this).ConfigureAwait(false);

            await _clientWrapper.SubscribeAsync(
                    Endpoint.Topics.Select(
                            topic =>
                                new MqttTopicFilterBuilder()
                                    .WithTopic(topic)
                                    .WithQualityOfServiceLevel(Endpoint.QualityOfServiceLevel)
                                    .Build())
                        .ToArray())
                .ConfigureAwait(false);
        }

        /// <inheritdoc cref="Consumer.DisconnectCoreAsync" />
        protected override async Task DisconnectCoreAsync()
        {
            if (_clientWrapper != null)
                await _clientWrapper.DisconnectAsync(this).ConfigureAwait(false);

            _clientWrapper?.Dispose();
            _clientWrapper = null;
        }

        /// <inheritdoc cref="Consumer.StartCoreAsync" />
        protected override Task StartCoreAsync()
        {
            if (_clientWrapper == null)
                throw new InvalidOperationException("The consumer is not connected.");

            _channelManager = new ConsumerChannelManager(_clientWrapper, _logger);
            _channelManager.StartReading();
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer.StopCoreAsync" />
        protected override Task StopCoreAsync()
        {
            _channelManager?.StopReading();
            _channelManager?.Dispose();
            _channelManager = null;

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer.WaitUntilConsumingStoppedAsync" />
        protected override Task WaitUntilConsumingStoppedAsync() =>
            _channelManager?.Stopping ?? Task.CompletedTask;

        /// <inheritdoc cref="Consumer.CommitCoreAsync" />
        protected override Task CommitCoreAsync(
            IReadOnlyCollection<MqttMessageIdentifier> brokerMessageIdentifiers) =>
            SetProcessingCompletedAsync(brokerMessageIdentifiers, true);

        /// <inheritdoc cref="Consumer.RollbackCoreAsync" />
        protected override Task RollbackCoreAsync(
            IReadOnlyCollection<MqttMessageIdentifier> brokerMessageIdentifiers) =>
            SetProcessingCompletedAsync(brokerMessageIdentifiers, false);

        private Task SetProcessingCompletedAsync(
            IReadOnlyCollection<MqttMessageIdentifier> brokerMessageIdentifiers,
            bool isSuccess)
        {
            Check.NotNull(brokerMessageIdentifiers, nameof(brokerMessageIdentifiers));

            var messageId = brokerMessageIdentifiers.Single().MessageId;

            if (_inProcessingMessage == null)
                return Task.CompletedTask;

            if (messageId != _inProcessingMessage.Id)
            {
                throw new InvalidOperationException(
                    "The committed message doesn't match with the current in processing message.");
            }

            _inProcessingMessage.TaskCompletionSource.SetResult(isSuccess);
            _inProcessingMessage = null;

            return Task.CompletedTask;
        }
    }
}
