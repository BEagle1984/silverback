using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Samples.Kafka.BatchWithTombstone.Common;

namespace Silverback.Samples.Kafka.BatchWithTombstone.Consumer;

public class SampleMessageBatchSubscriber
{
    private readonly ILogger<SampleMessageBatchSubscriber> _logger;

    public SampleMessageBatchSubscriber(ILogger<SampleMessageBatchSubscriber> logger)
    {
        _logger = logger;
    }

    public async Task OnBatchReceivedAsync(IAsyncEnumerable<IInboundEnvelope<SampleMessage>> batch)
    {
        int count = 0;

        await foreach (IInboundEnvelope<SampleMessage> envelope in batch)
        {
            if (envelope.IsTombstone)
            {
                _logger.LogInformation(
                    "Received tombstone message with key {Key}",
                    envelope.GetKafkaKey());
            }
            else
            {
                _logger.LogInformation(
                    "Received message with key {Key} and number {Number}",
                    envelope.GetKafkaKey(),
                    envelope.Message?.Number);
            }

            count++;
        }

        _logger.LogInformation("Received batch of {Count} messages", count);
    }
}
