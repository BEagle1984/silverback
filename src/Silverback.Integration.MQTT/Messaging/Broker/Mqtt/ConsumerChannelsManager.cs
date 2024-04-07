// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using Silverback.Diagnostics;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt;

internal class ConsumerChannelsManager : ConsumerChannelsManager<ConsumerChannel>
{
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Life cycle externally handled")]
    private readonly MqttConsumer _consumer;

    private readonly ISilverbackLogger _logger;

    private readonly ConsumerChannel[] _channels;

    private int _nextChannelIndex;

    public ConsumerChannelsManager(MqttConsumer consumer, ISilverbackLogger logger)
        : base(consumer, logger)
    {
        _consumer = Check.NotNull(consumer, nameof(consumer));
        _logger = Check.NotNull(logger, nameof(logger));

        _channels = Enumerable.Range(0, consumer.Configuration.MaxDegreeOfParallelism)
            .Select(index => new ConsumerChannel(consumer.Configuration.MaxDegreeOfParallelism, index, logger))
            .ToArray();

        consumer.Client.MessageReceived.AddHandler(OnMessageReceivedAsync);
    }

    public void StartReading() => _channels.ForEach(StartReading);

    protected override IEnumerable<ConsumerChannel> GetChannels() => _channels;

    protected override async Task ReadChannelOnceAsync(ConsumerChannel channel)
    {
        channel.ReadCancellationToken.ThrowIfCancellationRequested();

        ConsumedApplicationMessage consumedMessage = await channel.ReadAsync().ConfigureAwait(false);

        channel.ReadCancellationToken.ThrowIfCancellationRequested();

        // Clear the current activity to ensure we don't propagate the previous traceId
        Activity.Current = null;

        // Retry locally until successfully processed (or skipped)
        while (!channel.ReadCancellationToken.IsCancellationRequested)
        {
            await _consumer.HandleMessageAsync(consumedMessage, channel.SequenceStore).ConfigureAwait(false);

            if (await consumedMessage.TaskCompletionSource.Task.ConfigureAwait(false))
                break;

            channel.ReadCancellationToken.ThrowIfCancellationRequested();
            consumedMessage.TaskCompletionSource = new TaskCompletionSource<bool>();
        }
    }

    private async ValueTask OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
        ConsumedApplicationMessage receivedMessage = new(eventArgs);

        _logger.LogConsuming(receivedMessage, _consumer);

        eventArgs.AutoAcknowledge = false;
        await GetNextChannel().WriteAsync(receivedMessage, CancellationToken.None).ConfigureAwait(false);

        _nextChannelIndex = (_nextChannelIndex + 1) % _channels.Length;
    }

    private ConsumerChannel GetNextChannel()
    {
        ConsumerChannel channel = _channels[_nextChannelIndex++];

        if (channel.IsCompleted)
            channel.Reset();

        return channel;
    }
}
