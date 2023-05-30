// Copyright (c) 2020 Sergio Aquilini
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

namespace Silverback.Messaging.Broker.Mqtt
{
    internal sealed class ConsumerChannelManager : IDisposable
    {
        [SuppressMessage("", "CA2213", Justification = "Doesn't have to be disposed")]
        private readonly MqttClientWrapper _mqttClientWrapper;

        private readonly MqttConsumerEndpoint _endpoint;

        private readonly IInboundLogger<IConsumer> _logger;

        private readonly SemaphoreSlim _parallelismLimiterSemaphoreSlim;

        private Channel<ConsumedApplicationMessage> _channel;

        private CancellationTokenSource _readCancellationTokenSource;

        private TaskCompletionSource<bool> _readTaskCompletionSource;

        public ConsumerChannelManager(
            MqttClientWrapper mqttClientWrapper,
            MqttConsumerEndpoint endpoint,
            IInboundLogger<IConsumer> logger)
        {
            _mqttClientWrapper = Check.NotNull(mqttClientWrapper, nameof(mqttClientWrapper));
            _endpoint = Check.NotNull(endpoint, nameof(endpoint));
            _logger = Check.NotNull(logger, nameof(logger));

            _channel = CreateBoundedChannel();
            _parallelismLimiterSemaphoreSlim = new SemaphoreSlim(endpoint.MaxDegreeOfParallelism, endpoint.MaxDegreeOfParallelism);

            _readCancellationTokenSource = new CancellationTokenSource();
            _readTaskCompletionSource = new TaskCompletionSource<bool>();

            mqttClientWrapper.MqttClient.ApplicationMessageReceivedAsync += HandleApplicationMessageReceivedAsync;
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
            _channel.Writer.TryComplete();

            if (!IsReading)
                _readTaskCompletionSource.TrySetResult(true);
        }

        public async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            var receivedMessage = new ConsumedApplicationMessage(eventArgs);

            _logger.LogConsuming(receivedMessage, Consumer!);

            eventArgs.AutoAcknowledge = false;
            await _channel.Writer.WriteAsync(receivedMessage).ConfigureAwait(false);
        }

        public void Dispose()
        {
            StopReading();
            _mqttClientWrapper.MqttClient.ApplicationMessageReceivedAsync -= HandleApplicationMessageReceivedAsync;
            _readCancellationTokenSource.Dispose();
            _parallelismLimiterSemaphoreSlim.Dispose();
        }

        private Channel<ConsumedApplicationMessage> CreateBoundedChannel() =>
            Channel.CreateBounded<ConsumedApplicationMessage>(_endpoint.BackpressureLimit);

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task ReadChannelAsync()
        {
            // Clear the current activity to ensure we don't propagate the previous traceId
            Activity.Current = null;

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

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task ReadChannelOnceAsync()
        {
            _readCancellationTokenSource.Token.ThrowIfCancellationRequested();

            _logger.LogConsumerLowLevelTrace(Consumer, "Reading channel...");

            ConsumedApplicationMessage consumedMessage =
                await _channel.Reader.ReadAsync(_readCancellationTokenSource.Token).ConfigureAwait(false);

            await _parallelismLimiterSemaphoreSlim.WaitAsync(_readCancellationTokenSource.Token)
                .ConfigureAwait(false);

            Task.Run(
                async () =>
                {
                    try
                    {
                        await HandleMessageAsync(consumedMessage).ConfigureAwait(false);
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
                    finally
                    {
                        _parallelismLimiterSemaphoreSlim.Release();
                    }
                }).FireAndForget();
        }

        private async Task HandleMessageAsync(ConsumedApplicationMessage consumedMessage)
        {
            // Retry locally until successfully processed (or skipped)
            while (!_readCancellationTokenSource.Token.IsCancellationRequested)
            {
                await _mqttClientWrapper.HandleMessageAsync(consumedMessage).ConfigureAwait(false);

                if (await consumedMessage.TaskCompletionSource.Task.ConfigureAwait(false))
                {
                    await consumedMessage.EventArgs.AcknowledgeAsync(_readCancellationTokenSource.Token).ConfigureAwait(false);
                    break;
                }

                consumedMessage.TaskCompletionSource = new TaskCompletionSource<bool>();
            }
        }
    }
}
