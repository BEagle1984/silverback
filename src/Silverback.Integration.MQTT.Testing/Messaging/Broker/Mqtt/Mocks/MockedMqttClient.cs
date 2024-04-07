// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
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

    /// <inheritdoc cref="IMqttClient.InspectPackage" />
    public event Func<InspectMqttPacketEventArgs, Task>? InspectPackage;

    /// <inheritdoc cref="IMqttClient.IsConnected" />
    public bool IsConnected { get; private set; }

    /// <inheritdoc cref="IMqttClient.Options" />
    public MqttClientOptions? Options { get; private set; }

    /// <inheritdoc cref="IMqttClient.ConnectAsync" />
    public async Task<MqttClientConnectResult> ConnectAsync(MqttClientOptions options, CancellationToken cancellationToken = default)
    {
        Check.NotNull(options, nameof(options));

        Check.ThrowObjectDisposedIf(_isDisposed, this);

        if (_connecting)
            throw new InvalidOperationException("ConnectAsync shouldn't be called concurrently.");

        _connecting = true;

        Options = options;

        _broker.Connect(this);

        await Task.Delay(_mockOptions.ConnectionDelay, cancellationToken).ConfigureAwait(false);

        IsConnected = true;
        _connecting = false;

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

    /// <inheritdoc cref="IMqttClient.SubscribeAsync" />
    public Task<MqttClientSubscribeResult> SubscribeAsync(MqttClientSubscribeOptions options, CancellationToken cancellationToken = default)
    {
        Check.NotNull(options, nameof(options));
        Check.ThrowObjectDisposedIf(_isDisposed, this);

        _broker.Subscribe(this, options.TopicFilters.Select(filter => filter.Topic).ToList());

        return Task.FromResult(new MqttClientSubscribeResult());
    }

    /// <inheritdoc cref="IMqttClient.UnsubscribeAsync" />
    public Task<MqttClientUnsubscribeResult> UnsubscribeAsync(MqttClientUnsubscribeOptions options, CancellationToken cancellationToken = default)
    {
        Check.NotNull(options, nameof(options));
        Check.ThrowObjectDisposedIf(_isDisposed, this);

        _broker.Unsubscribe(this, options.TopicFilters);

        return Task.FromResult(new MqttClientUnsubscribeResult());
    }

    /// <inheritdoc cref="IMqttClient.PublishAsync" />
    public async Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken = default)
    {
        Check.NotNull(applicationMessage, nameof(applicationMessage));
        Check.ThrowObjectDisposedIf(_isDisposed, this);

        if (Options == null)
            throw new InvalidOperationException("The client is not connected.");

        await _broker.PublishAsync(this, applicationMessage, Options).ConfigureAwait(false);

        return new MqttClientPublishResult
        {
            ReasonCode = MqttClientPublishReasonCode.Success
        };
    }

    /// <inheritdoc cref="IMqttClient.PingAsync" />
    public Task PingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc cref="IMqttClient.SendExtendedAuthenticationExchangeDataAsync" />
    public Task SendExtendedAuthenticationExchangeDataAsync(
        MqttExtendedAuthenticationExchangeData data,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose() => _isDisposed = true;

    [SuppressMessage("Usage", "VSTHRD110", MessageId = "Observe result of async calls", Justification = "False positive")]
    internal Task HandleMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs) =>
        ApplicationMessageReceivedAsync?.Invoke(eventArgs) ?? Task.CompletedTask;
}
