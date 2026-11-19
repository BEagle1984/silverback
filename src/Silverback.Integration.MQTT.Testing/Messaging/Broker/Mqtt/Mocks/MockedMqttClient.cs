// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Diagnostics.PacketInspection;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt.Mocks;

/// <summary>
///     A mocked implementation of <see cref="IMqttClient" /> from MQTTnet that connects with an in-memory
///     broker.
/// </summary>
public sealed class MockedMqttClient : IMqttClient
{
    private readonly IInMemoryMqttBroker _broker;

    private readonly IMockedMqttOptions _mockOptions;

    private bool _connecting;

    private bool _isDisposed;

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
        _broker = Check.NotNull(broker);
        _mockOptions = Check.NotNull(mockOptions);
    }

    /// <inheritdoc cref="IMqttClient.ApplicationMessageReceivedAsync" />
    public event Func<MqttApplicationMessageReceivedEventArgs, Task>? ApplicationMessageReceivedAsync;

    /// <inheritdoc cref="IMqttClient.ConnectedAsync" />
    public event Func<MqttClientConnectedEventArgs, Task>? ConnectedAsync;

    /// <inheritdoc cref="IMqttClient.ConnectingAsync" />
    public event Func<MqttClientConnectingEventArgs, Task>? ConnectingAsync;

    /// <inheritdoc cref="IMqttClient.DisconnectedAsync" />
    public event Func<MqttClientDisconnectedEventArgs, Task>? DisconnectedAsync;

    /// <inheritdoc cref="IMqttClient.InspectPacketAsync" />
    public event Func<InspectMqttPacketEventArgs, Task>? InspectPacketAsync;

    /// <inheritdoc cref="IMqttClient.IsConnected" />
    public bool IsConnected { get; internal set; }

    /// <inheritdoc cref="IMqttClient.Options" />
    public MqttClientOptions? Options { get; private set; }

    /// <inheritdoc cref="IMqttClient.ConnectAsync" />
    public async Task<MqttClientConnectResult> ConnectAsync(MqttClientOptions options, CancellationToken cancellationToken = default)
    {
        Check.NotNull(options);

        Check.ThrowObjectDisposedIf(_isDisposed, this);

        if (_connecting)
            throw new InvalidOperationException("ConnectAsync shouldn't be called concurrently.");

        _connecting = true;

        try
        {
            Options = options;

            await Task.Delay(_mockOptions.ConnectionDelay, cancellationToken).ConfigureAwait(false);

            // The session is registered only once the connection succeeded, so that a canceled attempt leaves no trace. The broker
            // sets IsConnected together with the registration, like a real client that reports it only after the CONNACK.
            _broker.Connect(this);
        }
        finally
        {
            _connecting = false;
        }

        return new MqttClientConnectResult();
    }

    /// <inheritdoc cref="IMqttClient.DisconnectAsync" />
    public Task DisconnectAsync(MqttClientDisconnectOptions options, CancellationToken cancellationToken = default)
    {
        Check.ThrowObjectDisposedIf(_isDisposed, this);

        _broker.Disconnect(this);

        IsConnected = false;

        return Task.CompletedTask;
    }

    /// <inheritdoc cref="IMqttClient.SendEnhancedAuthenticationExchangeDataAsync" />
    public Task SendEnhancedAuthenticationExchangeDataAsync(MqttEnhancedAuthenticationExchangeData data, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    /// <inheritdoc cref="IMqttClient.SubscribeAsync" />
    public Task<MqttClientSubscribeResult> SubscribeAsync(MqttClientSubscribeOptions options, CancellationToken cancellationToken = default)
    {
        Check.NotNull(options);
        Check.ThrowObjectDisposedIf(_isDisposed, this);

        // Like the real client, reject the invalid topic filters before subscribing
        options.TopicFilters.ForEach(filter => MqttTopicValidator.ThrowIfInvalidSubscribe(filter.Topic));

        _broker.Subscribe(this, options.TopicFilters);

        return Task.FromResult(
            new MqttClientSubscribeResult(
                0,
                [.. options.TopicFilters.Select(filter => new MqttClientSubscribeResultItem(filter, GetGrantedResultCode(filter)))],
                "",
                []));
    }

    /// <inheritdoc cref="IMqttClient.UnsubscribeAsync" />
    public Task<MqttClientUnsubscribeResult> UnsubscribeAsync(MqttClientUnsubscribeOptions options, CancellationToken cancellationToken = default)
    {
        Check.NotNull(options);
        Check.ThrowObjectDisposedIf(_isDisposed, this);

        _broker.Unsubscribe(this, options.TopicFilters);

        return Task.FromResult(new MqttClientUnsubscribeResult(0, [], "", []));
    }

    /// <inheritdoc cref="IMqttClient.PublishAsync" />
    public async Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken = default)
    {
        Check.NotNull(applicationMessage);
        Check.ThrowObjectDisposedIf(_isDisposed, this);

        if (Options == null)
            throw new InvalidOperationException("The client is not connected.");

        await _broker.PublishAsync(this, applicationMessage, Options).ConfigureAwait(false);

        return new MqttClientPublishResult(0, MqttClientPublishReasonCode.Success, "", []);
    }

    /// <inheritdoc cref="IMqttClient.PingAsync" />
    public Task PingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose() => _isDisposed = true;

    // Called by the broker, after it reset IsConnected, when another client instance connects with the same client id. Like MQTTnet,
    // report the connection as lost: MQTT 5 brokers send a DISCONNECT packet with the session taken over reason, while MQTT 3.1.1
    // brokers just close the connection.
    internal void OnSessionTakenOver()
    {
        MqttClientDisconnectReason reason = Options?.ProtocolVersion >= MqttProtocolVersion.V500
            ? MqttClientDisconnectReason.SessionTakenOver
            : MqttClientDisconnectReason.NormalDisconnection;

        MqttClientDisconnectedEventArgs eventArgs = new(true, null, reason, "", [], null);

        Task.Run(() => DisconnectedAsync?.Invoke(eventArgs) ?? Task.CompletedTask).FireAndForget();
    }

    [SuppressMessage("Usage", "VSTHRD110", MessageId = "Observe result of async calls", Justification = "False positive")]
    internal Task HandleMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs) =>
        ApplicationMessageReceivedAsync?.Invoke(eventArgs) ?? Task.CompletedTask;

    // The mocked broker always grants the requested quality of service
    private static MqttClientSubscribeResultCode GetGrantedResultCode(MqttTopicFilter filter) =>
        filter.QualityOfServiceLevel switch
        {
            MqttQualityOfServiceLevel.AtLeastOnce => MqttClientSubscribeResultCode.GrantedQoS1,
            MqttQualityOfServiceLevel.ExactlyOnce => MqttClientSubscribeResultCode.GrantedQoS2,
            _ => MqttClientSubscribeResultCode.GrantedQoS0
        };
}
