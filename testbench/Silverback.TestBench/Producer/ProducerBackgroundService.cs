// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.TestBench.Producer.Messages;
using Silverback.TestBench.ViewModel;
using Silverback.TestBench.ViewModel.Logs;
using Silverback.TestBench.ViewModel.Topics;

namespace Silverback.TestBench.Producer;

public class ProducerBackgroundService : BackgroundService
{
    private readonly IPublisher _publisher;

    private readonly MessagesTracker _messagesTracker;

    private readonly MainViewModel _mainViewModel;

    private readonly ILogger<ProducerBackgroundService> _logger;

    public ProducerBackgroundService(
        IPublisher publisher,
        MessagesTracker messagesTracker,
        MainViewModel mainViewModel,
        ILogger<ProducerBackgroundService> logger)
    {
        _publisher = publisher;
        _messagesTracker = messagesTracker;
        _mainViewModel = mainViewModel;
        _logger = logger;
    }

    [SuppressMessage("Usage", "VSTHRD110:Observe result of async calls", Justification = "Fire and forget")]
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (TopicViewModel topic in _mainViewModel.Topics)
        {
            Task.Run(() => ProduceAsync(topic, stoppingToken), stoppingToken);
        }

        return Task.CompletedTask;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Log and continue in any case")]
    private async Task ProduceAsync(TopicViewModel topic, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_mainViewModel.IsProducing || !topic.IsEnabled)
            {
                await Task.Delay(1000, stoppingToken);
                continue;
            }

            using Activity activity = new("Produce");
            activity.Start();

            try
            {
                IReadOnlyCollection<IOutboundEnvelope<RoutableTestBenchMessage>> envelopes = await (topic switch
                {
                    KafkaTopicViewModel kafkaTopic =>
                        ProduceAsync(topic, () => new KafkaRoutableTestBenchMessage(kafkaTopic), stoppingToken),
                    MqttTopicViewModel mqttTopic =>
                        ProduceAsync(topic, () => new MqttRoutableTestBenchMessage(mqttTopic), stoppingToken),
                    _ => throw new InvalidOperationException($"Unsupported topic type: {topic.GetType().FullName}")
                });

                foreach (IOutboundEnvelope<RoutableTestBenchMessage> envelope in envelopes)
                {
                    _logger.LogInformation(
                        "Message {MessageId} was produced to {MessageDestination}",
                        envelope.Message?.MessageId,
                        envelope.GetEndpoint().RawName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Produce failed");
            }
        }
    }

    private async Task<IReadOnlyCollection<IOutboundEnvelope<RoutableTestBenchMessage>>> ProduceAsync<TMessage>(
        TopicViewModel topic,
        Func<TMessage> messageFactory,
        CancellationToken stoppingToken)
        where TMessage : RoutableTestBenchMessage
    {
        List<IOutboundEnvelope<RoutableTestBenchMessage>> envelopes = [];

        await _publisher.WrapAndPublishBatchAsync(
            GetMessagesToProduceAsync(topic, messageFactory, stoppingToken),
            (envelope, envelopesList) =>
            {
                envelope.SetKafkaDestinationTopic(envelope.Message?.TargetTopicName ?? "empty-topic-name");
                envelope.SetMqttDestinationTopic(envelope.Message?.TargetTopicName ?? "empty-topic-name");
                envelopesList.Add(envelope);
            },
            envelopes,
            stoppingToken);

        return envelopes;
    }

    private async IAsyncEnumerable<TMessage> GetMessagesToProduceAsync<TMessage>(
        TopicViewModel topic,
        Func<TMessage> messageFactory,
        [EnumeratorCancellation] CancellationToken stoppingToken)
        where TMessage : RoutableTestBenchMessage
    {
        int count = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            TMessage message = messageFactory();
            yield return message;

            _messagesTracker.TrackProduced(message);
            _mainViewModel.Trace.TraceProduced(
                message.MessageId,
                _mainViewModel.GetTopic(message.TargetTopicName),
                new LogEntry(DateTime.UtcNow, "Message produced", null));

            if (stoppingToken.IsCancellationRequested || !_mainViewModel.IsProducing || !topic.IsEnabled || count++ >= 1000)
                yield break;

            TimeSpan produceDelay = topic.ProduceDelay;
            double speedMultiplier = _mainViewModel.ProduceSpeedMultiplier;

            if (produceDelay == TimeSpan.Zero || !(speedMultiplier > 0))
                continue;

            TimeSpan produceSpeedMultiplier = produceDelay / speedMultiplier;
            await Task.Delay(produceSpeedMultiplier, stoppingToken);
        }
    }
}
