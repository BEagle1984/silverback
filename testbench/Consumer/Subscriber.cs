// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.TestBench.Consumer.Models;
using Silverback.TestBench.Models;

namespace Silverback.TestBench.Consumer;

public class Subscriber
{
    private readonly ILogger<Subscriber> _logger;

    private readonly ConcurrentDictionary<string, int> _failedAttemptsDictionary = new();

    public Subscriber(ILogger<Subscriber> logger)
    {
        _logger = logger;
    }

    public async Task OnMessageReceivedAsync(IInboundEnvelope<SingleMessage> envelope) =>
        await ProcessEnvelopeAsync(envelope);

    public async Task OnMessageReceivedAsync(IAsyncEnumerable<IInboundEnvelope<BatchMessage>> batch)
    {
        await foreach (IInboundEnvelope<TestBenchMessage> envelope in batch)
        {
            await ProcessEnvelopeAsync(envelope);
        }
    }

    public async Task OnMessageReceivedAsync(IAsyncEnumerable<IInboundEnvelope<UnboundedMessage>> stream)
    {
        await foreach (IInboundEnvelope<TestBenchMessage> envelope in stream)
        {
            await ProcessEnvelopeAsync(envelope);
        }
    }

    private async Task ProcessEnvelopeAsync(IInboundEnvelope<TestBenchMessage> envelope)
    {
        if (envelope.Message == null)
        {
            _logger.LogError("Message is null");
            return;
        }

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

        _logger.LogInformation(
            "Successfully processed message '{MessageId}' from topic '{TopicName}'",
            envelope.Message.MessageId,
            envelope.Endpoint.RawName);
    }
}
