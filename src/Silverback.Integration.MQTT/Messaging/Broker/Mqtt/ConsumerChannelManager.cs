// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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

        private readonly ISilverbackIntegrationLogger _logger;

        private Channel<ConsumedApplicationMessage> _channel;

        private CancellationTokenSource _readCancellationTokenSource;

        private TaskCompletionSource<bool> _readTaskCompletionSource;

        public ConsumerChannelManager(
            MqttClientWrapper mqttClientWrapper,
            ISilverbackIntegrationLogger logger)
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

        [SuppressMessage("", "VSTHRD110", Justification = Justifications.FireAndForget)]
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

            Task.Run(ReadChannelAsync);
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

            _logger.LogTrace(
                IntegrationEventIds.LowLevelTracing,
                "Writing message {messageId} from {topic} to channel.",
                receivedMessage.Id,
                receivedMessage.ApplicationMessage.Topic);

            await _channel.Writer.WriteAsync(receivedMessage).ConfigureAwait(false);

            while (!await receivedMessage.TaskCompletionSource.Task.ConfigureAwait(false))
            {
                await Task.Delay(10).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            StopReading();
            _readCancellationTokenSource.Dispose();
        }

        // TODO: Does backpressure limit make sense for MQTT? Will it push multiple messages?
        private static Channel<ConsumedApplicationMessage> CreateBoundedChannel() =>
            Channel.CreateBounded<ConsumedApplicationMessage>(10);

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task ReadChannelAsync()
        {
            try
            {
                _logger.LogTrace(
                    IntegrationEventIds.LowLevelTracing,
                    "Starting channel processing loop... (clientId: {clientId})",
                    _mqttClientWrapper.MqttClient.Options.ClientId);

                while (!_readCancellationTokenSource.IsCancellationRequested)
                {
                    await ReadChannelOnceAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore
                _logger.LogTrace(
                    IntegrationEventIds.LowLevelTracing,
                    "Exiting channel processing loop (operation canceled). (clientId: {clientId})",
                    _mqttClientWrapper.MqttClient.Options.ClientId);
            }
            catch (Exception ex)
            {
                if (!(ex is ConsumerPipelineFatalException))
                {
                    _logger.LogCritical(
                        IntegrationEventIds.ConsumerFatalError,
                        ex,
                        "Fatal error occurred processing the consumed message. The consumer will be stopped. (clientId: {clientId})",
                        _mqttClientWrapper.MqttClient.Options.ClientId);
                }

                IsReading = false;
                _readTaskCompletionSource.TrySetResult(false);

                await _mqttClientWrapper.Consumer!.DisconnectAsync().ConfigureAwait(false);
            }

            IsReading = false;
            _readTaskCompletionSource.TrySetResult(true);

            _logger.LogTrace(
                IntegrationEventIds.LowLevelTracing,
                "Exited channel processing loop. (clientId: {clientId})",
                _mqttClientWrapper.MqttClient.Options.ClientId);
        }

        private async Task ReadChannelOnceAsync()
        {
            _readCancellationTokenSource.Token.ThrowIfCancellationRequested();

            _logger.LogTrace(
                IntegrationEventIds.LowLevelTracing,
                "Reading channel... (clientId: {clientId})",
                _mqttClientWrapper.MqttClient.Options.ClientId);

            var consumedMessage = await _channel.Reader.ReadAsync(_readCancellationTokenSource.Token)
                .ConfigureAwait(false);

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
