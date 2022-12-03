// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt.Mocks
{
    /// <summary>
    ///     A mocked implementation of <see cref="IMqttClient" /> from MQTTnet that connects with an in-memory
    ///     broker.
    /// </summary>
    public sealed class MockedMqttClient : IMqttClient
    {
        private readonly IInMemoryMqttBroker _broker;

        private readonly IMockedMqttOptions _mockOptions;

        private bool _connecting;

        private bool _disposed;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MockedMqttClient" /> class.
        /// </summary>
        /// <param name="broker">
        ///     The <see cref="IInMemoryMqttBroker" />.
        /// </param>
        /// <param name="mockOptions">
        ///     The <see cref="IMockedMqttOptions" />.
        /// </param>
        public MockedMqttClient(IInMemoryMqttBroker broker, IMockedMqttOptions mockOptions)
        {
            _broker = Check.NotNull(broker, nameof(broker));
            _mockOptions = Check.NotNull(mockOptions, nameof(mockOptions));
        }

        /// <inheritdoc cref="IMqttClient.ApplicationMessageReceivedAsync" />
        public event Func<MqttApplicationMessageReceivedEventArgs, Task>? ApplicationMessageReceivedAsync;

        /// <inheritdoc cref="IMqttClient.ConnectedAsync" />
        public event Func<MqttClientConnectedEventArgs, Task>? ConnectedAsync;

        /// <inheritdoc cref="IMqttClient.ConnectingAsync" />
        public event Func<MqttClientConnectingEventArgs, Task>? ConnectingAsync;

        /// <inheritdoc cref="IMqttClient.DisconnectedAsync" />
        public event Func<MqttClientDisconnectedEventArgs, Task>? DisconnectedAsync;

#pragma warning disable CS0067
        /// <inheritdoc cref="IMqttClient.InspectPackage" />
        public event Func<InspectMqttPacketEventArgs, Task>? InspectPackage;
#pragma warning restore CS0067

        /// <summary>
        ///     Gets a value indicating whether the client is connected and a message handler is bound to it.
        /// </summary>
        public bool IsConsumerConnected =>
            ApplicationMessageReceivedAsync?.Target is ConsumerChannelManager consumerChannelManager &&
            (consumerChannelManager.Consumer?.IsConnected ?? false);

        /// <inheritdoc cref="IMqttClient.IsConnected" />
        public bool IsConnected { get; private set; }

        /// <inheritdoc cref="IMqttClient.Options" />
        public MqttClientOptions? Options { get; private set; }

        private string ClientId => Options?.ClientId ?? "(none)";

        /// <inheritdoc cref="IMqttClient.ConnectAsync" />
        public async Task<MqttClientConnectResult> ConnectAsync(
            MqttClientOptions options,
            CancellationToken cancellationToken)
        {
            Check.NotNull(options, nameof(options));

            EnsureNotDisposed();

            if (_connecting)
                throw new InvalidOperationException("ConnectAsync shouldn't be called concurrently.");

            _connecting = true;

            if (ConnectingAsync != null)
                await ConnectingAsync.Invoke(new MqttClientConnectingEventArgs(options)).ConfigureAwait(false);

            Options = options;
            _broker.Connect(options, this);

            await Task.Delay(_mockOptions.ConnectionDelay, cancellationToken).ConfigureAwait(false);

            IsConnected = true;
            _connecting = false;

            if (ConnectedAsync != null)
                await ConnectedAsync.Invoke(new MqttClientConnectedEventArgs(new MqttClientConnectResult())).ConfigureAwait(false);

            return new MqttClientConnectResult();
        }

        /// <inheritdoc cref="IMqttClient.DisconnectAsync" />
        public async Task DisconnectAsync(MqttClientDisconnectOptions options, CancellationToken cancellationToken)
        {
            EnsureNotDisposed();

            _broker.Disconnect(ClientId);

            IsConnected = false;

            if (DisconnectedAsync != null)
                await DisconnectedAsync.Invoke(new MqttClientDisconnectedEventArgs()).ConfigureAwait(false);
        }

        /// <inheritdoc cref="IMqttClient.SubscribeAsync" />
        public Task<MqttClientSubscribeResult> SubscribeAsync(
            MqttClientSubscribeOptions options,
            CancellationToken cancellationToken)
        {
            Check.NotNull(options, nameof(options));
            EnsureNotDisposed();

            _broker.Subscribe(ClientId, options.TopicFilters.Select(filter => filter.Topic).ToList());

            return Task.FromResult(new MqttClientSubscribeResult());
        }

        /// <inheritdoc cref="IMqttClient.UnsubscribeAsync" />
        public Task<MqttClientUnsubscribeResult> UnsubscribeAsync(
            MqttClientUnsubscribeOptions options,
            CancellationToken cancellationToken)
        {
            Check.NotNull(options, nameof(options));
            EnsureNotDisposed();

            _broker.Unsubscribe(ClientId, options.TopicFilters);

            return Task.FromResult(new MqttClientUnsubscribeResult());
        }

        /// <inheritdoc cref="IMqttClient.PublishAsync" />
        public async Task<MqttClientPublishResult> PublishAsync(
            MqttApplicationMessage applicationMessage,
            CancellationToken cancellationToken)
        {
            Check.NotNull(applicationMessage, nameof(applicationMessage));
            EnsureNotDisposed();

            if (Options == null)
                throw new InvalidOperationException("The client is not connected.");

            await _broker.PublishAsync(ClientId, applicationMessage, Options).ConfigureAwait(false);

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
            _disposed = true;
        }

        internal Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs eventArgs) =>
            ApplicationMessageReceivedAsync?.Invoke(eventArgs) ?? Task.CompletedTask;

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }
    }
}
