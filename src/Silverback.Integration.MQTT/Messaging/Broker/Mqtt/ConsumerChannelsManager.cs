// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using Silverback.Diagnostics;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Sequences;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt
{
    internal sealed class ConsumerChannelsManager : ConsumerChannelsManager<ConsumedApplicationMessage>
    {
        [SuppressMessage("", "CA2213", Justification = "Doesn't have to be disposed")]
        private readonly MqttClientWrapper _mqttClientWrapper;

        private readonly IInboundLogger<IConsumer> _logger;

        private readonly SemaphoreSlim _readingSemaphoreSlim = new(1, 1);

        private readonly TaskCompletionSource<bool>[] _readTaskCompletionSources;

        private CancellationTokenSource _readCancellationTokenSource;

        private int _nextChannelIndex;

        public ConsumerChannelsManager(
            MqttClientWrapper mqttClientWrapper,
            MqttConsumerEndpoint endpoint,
            Func<ISequenceStore> sequenceStoreFactory,
            IInboundLogger<IConsumer> logger)
            : base(
                endpoint.MaxDegreeOfParallelism,
                endpoint.BackpressureLimit,
                sequenceStoreFactory)
        {
            _mqttClientWrapper = Check.NotNull(mqttClientWrapper, nameof(mqttClientWrapper));
            _logger = Check.NotNull(logger, nameof(logger));

            _readCancellationTokenSource = new CancellationTokenSource();
            _readTaskCompletionSources = Enumerable.Range(0, Channels.Length)
                .Select(_ => new TaskCompletionSource<bool>())
                .ToArray();

            mqttClientWrapper.MqttClient.ApplicationMessageReceivedAsync += HandleApplicationMessageReceivedAsync;
        }

        public MqttConsumer? Consumer => _mqttClientWrapper.Consumer;

        public Task Stopping =>
            Task.WhenAll(_readTaskCompletionSources.Select(taskCompletionSource => taskCompletionSource.Task));

        public bool IsReading { get; private set; }

        public void StartReading()
        {
            _readingSemaphoreSlim.Wait();

            try
            {
                if (IsReading)
                    return;

                IsReading = true;

                if (_readCancellationTokenSource.IsCancellationRequested)
                {
                    _readCancellationTokenSource.Dispose();
                    _readCancellationTokenSource = new CancellationTokenSource();
                }

                for (var i = 0; i < Channels.Length; i++)
                {
                    StartReading(i);
                }
            }
            finally
            {
                _readingSemaphoreSlim.Release();
            }
        }

        public async Task StopReadingAsync()
        {
            await _readingSemaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                _readCancellationTokenSource.Cancel();

                if (!IsReading)
                    _readTaskCompletionSources.ForEach(source => source.TrySetResult(true));

                await SequenceStores.ParallelForEachAsync(store => store.AbortAllAsync(SequenceAbortReason.ConsumerAborted)).ConfigureAwait(false);
            }
            finally
            {
                _readingSemaphoreSlim.Release();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
                return;

            AsyncHelper.RunSynchronously(StopReadingAsync);
            _mqttClientWrapper.MqttClient.ApplicationMessageReceivedAsync -= HandleApplicationMessageReceivedAsync;

            // If we don't close the channel, the client will be unable to disconnect
            Channels.ForEach(channel => channel.Writer.TryComplete());

            _readCancellationTokenSource.Dispose();
            _readingSemaphoreSlim.Dispose();
        }

        private void StartReading(int channelIndex)
        {
            if (_readTaskCompletionSources[channelIndex].Task.IsCompleted)
                _readTaskCompletionSources[channelIndex] = new TaskCompletionSource<bool>();

            // Clear the current activity to ensure we don't propagate the previous traceId (e.g. when restarting because of a rollback)
            Activity.Current = null;

            Task.Run(() => ReadChannelAsync(channelIndex)).FireAndForget();
        }

        private async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            var receivedMessage = new ConsumedApplicationMessage(eventArgs);

            _logger.LogConsuming(receivedMessage, Consumer!);

            eventArgs.AutoAcknowledge = false;
            await Channels[_nextChannelIndex].Writer.WriteAsync(receivedMessage).ConfigureAwait(false);

            _nextChannelIndex = (_nextChannelIndex + 1) % Channels.Length;
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task ReadChannelAsync(int channelIndex)
        {
            try
            {
                _logger.LogConsumerLowLevelTrace(
                    Consumer,
                    "Starting channel {channelIndex} processing loop...",
                    () => new object[] { channelIndex });

                while (!_readCancellationTokenSource.IsCancellationRequested)
                {
                    await ReadChannelOnceAsync(channelIndex).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore
                _logger.LogConsumerLowLevelTrace(
                    Consumer,
                    "Exiting channel {channelIndex} processing loop (operation canceled).",
                    () => new object[] { channelIndex });
            }
            catch (Exception ex)
            {
                if (ex is not ConsumerPipelineFatalException)
                    _logger.LogConsumerFatalError(Consumer, ex);

                IsReading = false;
                _readTaskCompletionSources[channelIndex].TrySetResult(false);

                await _mqttClientWrapper.Consumer!.DisconnectAsync().ConfigureAwait(false);
            }

            IsReading = false;
            _readTaskCompletionSources[channelIndex].TrySetResult(true);

            _logger.LogConsumerLowLevelTrace(
                Consumer,
                "Exited channel {channelIndex} processing loop.",
                () => new object[] { channelIndex });
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task ReadChannelOnceAsync(int channelIndex)
        {
            if (Consumer == null)
                throw new InvalidOperationException("No consumer was bound.");

            _readCancellationTokenSource.Token.ThrowIfCancellationRequested();

            _logger.LogConsumerLowLevelTrace(Consumer, "Reading channel...");

            ConsumedApplicationMessage consumedMessage =
                await Channels[channelIndex].Reader.ReadAsync(_readCancellationTokenSource.Token).ConfigureAwait(false);

            // Retry locally until successfully processed (or skipped)
            while (!_readCancellationTokenSource.Token.IsCancellationRequested)
            {
                await Consumer.HandleMessageAsync(consumedMessage, SequenceStores[channelIndex]).ConfigureAwait(false);

                if (await consumedMessage.TaskCompletionSource.Task.ConfigureAwait(false))
                    break;

                consumedMessage.TaskCompletionSource = new TaskCompletionSource<bool>();
            }
        }
    }
}
