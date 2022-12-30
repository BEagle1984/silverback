// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MQTTnet.Client;
using Silverback.Diagnostics;
using Silverback.Messaging.Diagnostics;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt;

internal sealed class ConsumerChannelManager : IDisposable
{
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Life cycle externally handled")]
    private readonly MqttConsumer _consumer;

    private readonly IConsumerLogger<IConsumer> _logger;

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Life cycle externally handled")]
    private readonly IMqttClientWrapper _mqttClientWrapper;

    // The parallelism is currently fixed to 1 but could be made configurable for QoS=0.
    // QoS=1+ requires the acks to come in the right order so better not mess with it (in the normal case
    // the broker doesn't even send the next message before the ack is received and increasing this value
    // will only cause trouble in edge cases like ack timeouts).
    private readonly SemaphoreSlim _parallelismLimiterSemaphoreSlim = new(1, 1);

    private Channel<ConsumedApplicationMessage> _channel;

    private CancellationTokenSource _readCancellationTokenSource;

    private TaskCompletionSource<bool> _readTaskCompletionSource;

    public ConsumerChannelManager(MqttConsumer consumer, IConsumerLogger<IConsumer> logger)
    {
        _consumer = Check.NotNull(consumer, nameof(consumer));
        _logger = Check.NotNull(logger, nameof(logger));

        _channel = CreateBoundedChannel();

        _readCancellationTokenSource = new CancellationTokenSource();
        _readTaskCompletionSource = new TaskCompletionSource<bool>();

        _mqttClientWrapper = consumer.Client;
        _mqttClientWrapper.MessageReceived.AddHandler(OnMessageReceivedAsync);
    }

    public Task Stopping => _readTaskCompletionSource.Task;

    public bool IsReading { get; private set; }

    public void StartReading()
    {
        if (IsReading)
            return;

        IsReading = true;

        if (_readCancellationTokenSource.IsCancellationRequested)
        {
            _readCancellationTokenSource.Dispose();
            _readCancellationTokenSource = new CancellationTokenSource();
        }

        if (_readTaskCompletionSource.Task.IsCompleted)
            _readTaskCompletionSource = new TaskCompletionSource<bool>();

        EnsureChannelReady();

        Task.Run(ReadChannelAsync).FireAndForget();
    }

    public void StopReading()
    {
        _readCancellationTokenSource.Cancel();
        _channel.Writer.TryComplete();

        if (!IsReading)
            _readTaskCompletionSource.TrySetResult(true);
    }

    public void Dispose()
    {
        _mqttClientWrapper.MessageReceived.RemoveHandler(OnMessageReceivedAsync);
        StopReading();
        _readCancellationTokenSource.Dispose();
        _parallelismLimiterSemaphoreSlim.Dispose();
    }

    private static Channel<ConsumedApplicationMessage> CreateBoundedChannel() => Channel.CreateBounded<ConsumedApplicationMessage>(10);

    private void EnsureChannelReady()
    {
        if (_channel.Reader.Completion.IsCompleted)
            _channel = CreateBoundedChannel();
    }

    private async ValueTask OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
        ConsumedApplicationMessage receivedMessage = new(eventArgs.ApplicationMessage);

        _logger.LogConsuming(receivedMessage, _consumer);

        EnsureChannelReady();

        await _channel.Writer.WriteAsync(receivedMessage).ConfigureAwait(false);

        // Wait until the processing is over, including retries
        while (!await receivedMessage.TaskCompletionSource.Task.ConfigureAwait(false))
        {
            await Task.Delay(10).ConfigureAwait(false);
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    private async Task ReadChannelAsync()
    {
        // Clear the current activity to ensure we don't propagate the previous traceId
        Activity.Current = null;
        try
        {
            _logger.LogConsumerLowLevelTrace(_consumer, "Starting channel processing loop...");

            while (!_readCancellationTokenSource.IsCancellationRequested)
            {
                await ReadChannelOnceAsync().ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore
            _logger.LogConsumerLowLevelTrace(_consumer, "Exiting channel processing loop (operation canceled).");
        }
        catch (Exception ex)
        {
            if (ex is not ConsumerPipelineFatalException)
                _logger.LogConsumerFatalError(_consumer, ex);

            IsReading = false;
            _readTaskCompletionSource.TrySetResult(false);

            await _mqttClientWrapper.DisconnectAsync().ConfigureAwait(false);
        }

        IsReading = false;
        _readTaskCompletionSource.TrySetResult(true);

        _logger.LogConsumerLowLevelTrace(_consumer, "Exited channel processing loop.");
    }

    private async Task ReadChannelOnceAsync()
    {
        _readCancellationTokenSource.Token.ThrowIfCancellationRequested();

        _logger.LogConsumerLowLevelTrace(_consumer, "Reading channel...");

        ConsumedApplicationMessage consumedMessage =
            await _channel.Reader.ReadAsync(_readCancellationTokenSource.Token).ConfigureAwait(false);

        await _parallelismLimiterSemaphoreSlim.WaitAsync(_readCancellationTokenSource.Token)
            .ConfigureAwait(false);

        try
        {
            await HandleMessageAsync(consumedMessage).ConfigureAwait(false);
        }
        finally
        {
            _parallelismLimiterSemaphoreSlim.Release();
        }
    }

    private async Task HandleMessageAsync(ConsumedApplicationMessage consumedMessage)
    {
        // Clear the current activity to ensure we don't propagate the previous traceId
        Activity.Current = null;

        // Retry locally until successfully processed (or skipped)
        while (!_readCancellationTokenSource.Token.IsCancellationRequested)
        {
            await _consumer.HandleMessageAsync(consumedMessage).ConfigureAwait(false);

            if (await consumedMessage.TaskCompletionSource.Task.ConfigureAwait(false))
                break;

            consumedMessage.TaskCompletionSource = new TaskCompletionSource<bool>();
        }
    }
}
