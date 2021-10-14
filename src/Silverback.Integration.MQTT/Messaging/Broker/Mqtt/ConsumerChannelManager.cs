// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client.Receiving;
using Silverback.Diagnostics;
using Silverback.Messaging.Diagnostics;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt
{
    internal sealed class ConsumerChannelManager : IMqttApplicationMessageReceivedHandler, IDisposable
    {
        [SuppressMessage("", "CA2213", Justification = "Doesn't have to be disposed")]
        private readonly MqttClientWrapper _mqttClientWrapper;

        private readonly IInboundLogger<IConsumer> _logger;

        // The parallelism is currently fixed to 1 but could be made configurable for QoS=0.
        // QoS=1+ requires the acks to come in the right order so better not mess with it (in the normal case
        // the broker doesn't even send the next message before the ack is received and increasing this value
        // will only cause trouble in edge cases like ack timeouts).
        private readonly SemaphoreSlim _parallelismLimiterSemaphoreSlim = new(1, 1);

        private Channel<ConsumedApplicationMessage> _channel;

        private CancellationTokenSource _readCancellationTokenSource;

        private TaskCompletionSource<bool> _readTaskCompletionSource;

        public ConsumerChannelManager(
            MqttClientWrapper mqttClientWrapper,
            IInboundLogger<IConsumer> logger)
        {
            _mqttClientWrapper = Check.NotNull(mqttClientWrapper, nameof(mqttClientWrapper));
            _logger = Check.NotNull(logger, nameof(logger));

            _channel = CreateBoundedChannel();

            _readCancellationTokenSource = new CancellationTokenSource();
            _readTaskCompletionSource = new TaskCompletionSource<bool>();

            mqttClientWrapper.MqttClient.ApplicationMessageReceivedHandler = this;
        }

        public MqttConsumer? Consumer => _mqttClientWrapper.Consumer;

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

            if (_channel.Reader.Completion.IsCompleted)
                _channel = CreateBoundedChannel();

            Task.Run(ReadChannelAsync).FireAndForget();
        }

        public void StopReading()
        {
            _readCancellationTokenSource.Cancel();

            if (!IsReading)
                _readTaskCompletionSource.TrySetResult(true);
        }

        public async Task HandleApplicationMessageReceivedAsync(
            MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            var receivedMessage = new ConsumedApplicationMessage(eventArgs.ApplicationMessage);

            _logger.LogConsuming(receivedMessage, Consumer!);

            await _channel.Writer.WriteAsync(receivedMessage).ConfigureAwait(false);

            // Wait until the processing is over, including retries
            while (!await receivedMessage.TaskCompletionSource.Task.ConfigureAwait(false))
            {
                await Task.Delay(10).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            StopReading();
            _readCancellationTokenSource.Dispose();
            _parallelismLimiterSemaphoreSlim.Dispose();
        }

        private static Channel<ConsumedApplicationMessage> CreateBoundedChannel() =>
            Channel.CreateBounded<ConsumedApplicationMessage>(10);

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task ReadChannelAsync()
        {
            try
            {
                _logger.LogConsumerLowLevelTrace(Consumer, "Starting channel processing loop...");

                while (!_readCancellationTokenSource.IsCancellationRequested)
                {
                    await ReadChannelOnceAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore
                _logger.LogConsumerLowLevelTrace(
                    Consumer,
                    "Exiting channel processing loop (operation canceled).");
            }
            catch (Exception ex)
            {
                if (ex is not ConsumerPipelineFatalException)
                    _logger.LogConsumerFatalError(Consumer, ex);

                IsReading = false;
                _readTaskCompletionSource.TrySetResult(false);

                await _mqttClientWrapper.Consumer!.DisconnectAsync().ConfigureAwait(false);
            }

            IsReading = false;
            _readTaskCompletionSource.TrySetResult(true);

            _logger.LogConsumerLowLevelTrace(Consumer, "Exited channel processing loop.");
        }

        private async Task ReadChannelOnceAsync()
        {
            _readCancellationTokenSource.Token.ThrowIfCancellationRequested();

            _logger.LogConsumerLowLevelTrace(Consumer, "Reading channel...");

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
            // Retry locally until successfully processed (or skipped)
            while (!_readCancellationTokenSource.Token.IsCancellationRequested)
            {
                await _mqttClientWrapper.HandleMessageAsync(consumedMessage).ConfigureAwait(false);

                if (await consumedMessage.TaskCompletionSource.Task.ConfigureAwait(false))
                    break;

                consumedMessage.TaskCompletionSource = new TaskCompletionSource<bool>();
            }
        }
    }
}
