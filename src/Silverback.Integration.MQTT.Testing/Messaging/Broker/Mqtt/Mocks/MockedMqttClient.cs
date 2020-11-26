// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.ExtendedAuthenticationExchange;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;
using MQTTnet.Protocol;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt.Mocks
{
    /// <summary>
    ///     A mocked implementation of <see cref="IMqttClient" /> from MQTTnet that connects with an in-memory
    ///     broker.
    /// </summary>
    public sealed class MockedMqttClient : IMqttClient, IMqttApplicationMessageReceivedHandler
    {
        private readonly IInMemoryMqttBroker _broker;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MockedMqttClient" /> class.
        /// </summary>
        /// <param name="broker">
        ///     The <see cref="IInMemoryMqttBroker" />.
        /// </param>
        public MockedMqttClient(IInMemoryMqttBroker broker)
        {
            _broker = Check.NotNull(broker, nameof(broker));
        }

        /// <inheritdoc cref="IMqttClient.IsConnected" />
        public bool IsConnected { get; private set; }

        /// <inheritdoc cref="IMqttClient.Options" />
        public IMqttClientOptions? Options { get; private set; }

        /// <inheritdoc cref="IApplicationMessageReceiver.ApplicationMessageReceivedHandler" />
        public IMqttApplicationMessageReceivedHandler? ApplicationMessageReceivedHandler { get; set; }

        /// <inheritdoc cref="IMqttClient.ConnectedHandler" />
        public IMqttClientConnectedHandler? ConnectedHandler { get; set; }

        /// <inheritdoc cref="IMqttClient.DisconnectedHandler" />
        public IMqttClientDisconnectedHandler? DisconnectedHandler { get; set; }

        private string ClientId => Options?.ClientId ?? "(none)";

        /// <inheritdoc cref="IMqttClient.ConnectAsync" />
        public Task<MqttClientAuthenticateResult> ConnectAsync(
            IMqttClientOptions options,
            CancellationToken cancellationToken)
        {
            Check.NotNull(options, nameof(options));

            Options = options;

            _broker.Connect(options, this);

            IsConnected = true;

            return Task.FromResult(new MqttClientAuthenticateResult());
        }

        /// <inheritdoc cref="IMqttClient.DisconnectAsync" />
        public Task DisconnectAsync(MqttClientDisconnectOptions options, CancellationToken cancellationToken)
        {
            _broker.Disconnect(ClientId);

            IsConnected = false;

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="IMqttClient.SubscribeAsync" />
        public Task<MqttClientSubscribeResult> SubscribeAsync(
            MqttClientSubscribeOptions options,
            CancellationToken cancellationToken)
        {
            Check.NotNull(options, nameof(options));

            _broker.Subscribe(ClientId, options.TopicFilters.Select(filter => filter.Topic).ToList());

            var result = new MqttClientSubscribeResult();
            options.TopicFilters.ForEach(filter => result.Items.Add(MapSubscribeResultItem(filter)));
            return Task.FromResult(result);
        }

        /// <inheritdoc cref="IMqttClient.UnsubscribeAsync" />
        public Task<MqttClientUnsubscribeResult> UnsubscribeAsync(
            MqttClientUnsubscribeOptions options,
            CancellationToken cancellationToken)
        {
            Check.NotNull(options, nameof(options));

            _broker.Unsubscribe(ClientId, options.TopicFilters);

            var result = new MqttClientUnsubscribeResult();
            options.TopicFilters.ForEach(filter => result.Items.Add(MapUnsubscribeResultItem(filter)));
            return Task.FromResult(result);
        }

        /// <inheritdoc cref="IApplicationMessagePublisher.PublishAsync" />
        public async Task<MqttClientPublishResult> PublishAsync(
            MqttApplicationMessage applicationMessage,
            CancellationToken cancellationToken)
        {
            Check.NotNull(applicationMessage, nameof(applicationMessage));

            await _broker.PublishAsync(ClientId, applicationMessage).ConfigureAwait(false);

            return new MqttClientPublishResult
            {
                ReasonCode = MqttClientPublishReasonCode.Success
            };
        }

        /// <inheritdoc cref="IMqttClient.PingAsync" />
        public Task PingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <inheritdoc cref="IMqttClient.SendExtendedAuthenticationExchangeDataAsync" />
        public Task SendExtendedAuthenticationExchangeDataAsync(
            MqttExtendedAuthenticationExchangeData data,
            CancellationToken cancellationToken) => Task.CompletedTask;

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            // Nothing to dispose
        }

        /// <inheritdoc cref="IMqttApplicationMessageReceivedHandler.HandleApplicationMessageReceivedAsync" />
        public Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs) =>
            ApplicationMessageReceivedHandler?.HandleApplicationMessageReceivedAsync(eventArgs) ?? Task.CompletedTask;

        private static MqttClientSubscribeResultItem MapSubscribeResultItem(MqttTopicFilter topicFilter)
        {
            MqttClientSubscribeResultCode resultCode;

            switch (topicFilter.QualityOfServiceLevel)
            {
                case MqttQualityOfServiceLevel.AtMostOnce:
                    resultCode = MqttClientSubscribeResultCode.GrantedQoS0;
                    break;
                case MqttQualityOfServiceLevel.AtLeastOnce:
                    resultCode = MqttClientSubscribeResultCode.GrantedQoS1;
                    break;
                case MqttQualityOfServiceLevel.ExactlyOnce:
                    resultCode = MqttClientSubscribeResultCode.GrantedQoS2;
                    break;
                default:
                    throw new InvalidOperationException("Invalid QualityOfServiceLevel.");
            }

            return new MqttClientSubscribeResultItem(topicFilter, resultCode);
        }

        private static MqttClientUnsubscribeResultItem MapUnsubscribeResultItem(string topicFilter)
        {
            return new MqttClientUnsubscribeResultItem(topicFilter, MqttClientUnsubscribeResultCode.Success);
        }
    }
}
