// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.TestBench.Consumer.Models;
using Silverback.TestBench.Models;

namespace Silverback.TestBench.Consumer;

public class Subscriber
{
    private readonly IPublisher _publisher;

    private readonly ILogger<Subscriber> _logger;

    private readonly ConcurrentDictionary<string, int> _failedAttemptsDictionary = new();

    private readonly ConcurrentDictionary<TopicPartition, int> _batchConsecutiveFailures = new();

    public Subscriber(IPublisher publisher, ILogger<Subscriber> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task OnMessageReceivedAsync(IInboundEnvelope<SingleMessage> envelope)
    {
        await ProcessEnvelopeAsync(envelope);
        LogMessageProcessed(envelope);
    }

    public async Task OnMessageReceivedAsync(IAsyncEnumerable<IInboundEnvelope<BatchMessage>> batch)
    {
        TopicPartition? partition = null;

        try
        {
            List<IInboundEnvelope<TestBenchMessage>> envelopes = [];

            int consecutiveFailures = 0;

            await foreach (IInboundEnvelope<TestBenchMessage> envelope in batch)
            {
                if (partition == null)
                {
                    partition = envelope.GetKafkaOffset().TopicPartition;
                    consecutiveFailures = _batchConsecutiveFailures.AddOrUpdate(partition, _ => 0, (_, _) => 0);
                }

                // Prevent too many consecutive failures to avoid whole batches being skipped
                if (consecutiveFailures > 9)
                    envelope.Message?.SimulatedFailuresCount = 0;

                await ProcessEnvelopeAsync(envelope);
                envelopes.Add(envelope);
                LogMessageEnumerated(envelope);
            }

            foreach (IInboundEnvelope<TestBenchMessage> envelope in envelopes)
            {
                LogMessageProcessed(envelope);
            }
        }
        catch (Exception)
        {
            if (partition != null)
                _batchConsecutiveFailures.AddOrUpdate(partition, _ => 1, (_, count) => count + 1);

            throw;
        }
    }

    public async Task OnMessageReceivedAsync(IEnumerable<IInboundEnvelope<BatchMessage2>> batch)
    {
        List<IInboundEnvelope<BatchMessage2>> envelopes = [.. batch];

        await _publisher.WrapAndPublishBatchAsync(
            envelopes.Where(envelope => envelope.Message != null).Select(envelope => envelope.Message!),
            message => new KafkaResponseMessage { MessageId = message.MessageId },
            (envelope, message) => envelope.SetKafkaKey(message.MessageId));

        foreach (IInboundEnvelope<TestBenchMessage> envelope in envelopes)
        {
            LogMessageProcessed(envelope);
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Intentional swallowing")]
    public async Task OnMessageReceivedAsync(IAsyncEnumerable<IInboundEnvelope<UnboundedMessage>> stream)
    {
        IInboundEnvelope<TestBenchMessage>? firstEnvelope = null;

        await foreach (IInboundEnvelope<TestBenchMessage> envelope in stream)
        {
            firstEnvelope ??= envelope;

            try
            {
                await ProcessEnvelopeAsync(envelope);
            }
            catch (Exception ex)
            {
                // Swallow because streaming doesn't support error policies
                _logger.LogInformation(
                    ex,
                    "Ignoring error processing message {MessageId} from topic {TopicName}",
                    envelope.Message?.MessageId,
                    envelope.Endpoint.RawName);
            }

            LogMessageProcessed(envelope);
        }
    }

    public void OnMessageSkipped(MessageSkipped skipped) =>
        _logger.LogInformation("Message {MessageId} from topic {TopicName} skipped via error policy", skipped.MessageId, skipped.TopicName);

    private async Task ProcessEnvelopeAsync(IInboundEnvelope<TestBenchMessage> envelope)
    {
        if (envelope.Message == null)
        {
            _logger.LogError("Message is null");
            return;
        }

        if (envelope.Message.SimulatedProcessingTime > TimeSpan.Zero)
            await Task.Delay(envelope.Message.SimulatedProcessingTime);

        if (envelope.Message.SimulatedFailuresCount > 0)
        {
            int failedAttempts = _failedAttemptsDictionary.AddOrUpdate(
                envelope.Message.MessageId,
                _ => 1,
                (_, count) => count + 1);

            if (envelope.Message.SimulatedFailuresCount >= failedAttempts)
            {
                throw new SimulatedFailureException(
                    $"Simulating exception processing message '{envelope.Message.MessageId}' from topic '{envelope.Endpoint.RawName}'. " +
                    $"(SimulatedFailuresCount={envelope.Message.SimulatedFailuresCount}, " +
                    $"FailedAttempts={envelope.Headers.GetValueOrDefault<int>(DefaultMessageHeaders.FailedAttempts)})");
            }
        }

        switch (envelope.Endpoint)
        {
            case KafkaConsumerEndpoint:
                await _publisher.PublishAsync(new KafkaResponseMessage { MessageId = envelope.Message.MessageId });
                break;
            case MqttConsumerEndpoint:
                await _publisher.PublishAsync(new MqttResponseMessage { MessageId = envelope.Message.MessageId });
                break;
        }
    }

    private void LogMessageProcessed(IInboundEnvelope<TestBenchMessage> envelope) =>
        _logger.LogInformation(
            "Successfully processed message {MessageId} from topic {TopicName}",
            envelope.Message?.MessageId,
            envelope.Endpoint.RawName);

    private void LogMessageEnumerated(IInboundEnvelope<TestBenchMessage> envelope) =>
        _logger.LogInformation(
            "Enumerated message {MessageId} from topic {TopicName}",
            envelope.Message?.MessageId,
            envelope.Endpoint.RawName);
}
