// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.TestBench.Models;

namespace Silverback.TestBench.Consumer;

public class Subscriber
{
    private readonly ILogger<Subscriber> _logger;

    public Subscriber(ILogger<Subscriber> logger)
    {
        _logger = logger;
    }

    public async Task OnMessageReceivedAsync(IInboundEnvelope<TestBenchMessage> envelope)
    {
        if (envelope.Message == null)
        {
            _logger.LogError("Message is null");
            return;
        }

        await Task.Delay(envelope.Message.SimulatedProcessingTime);
        _logger.LogInformation(
            "Successfully processed message '{MessageId}' from topic '{TopicName}'",
            envelope.Message.MessageId,
            envelope.Endpoint.RawName);
    }

    public async Task OnMessageReceivedAsync(IAsyncEnumerable<IInboundEnvelope<TestBenchMessage>> batch)
    {
        await foreach (IInboundEnvelope<TestBenchMessage> envelope in batch)
        {
            await OnMessageReceivedAsync(envelope);
        }
    }
}
