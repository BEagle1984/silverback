using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Samples.Kafka.Batch.Common;

namespace Silverback.Samples.Kafka.Batch.Consumer
{
    public class SampleMessageBatchSubscriber
    {
        private readonly ILogger<SampleMessageBatchSubscriber> _logger;

        public SampleMessageBatchSubscriber(
            ILogger<SampleMessageBatchSubscriber> logger)
        {
            _logger = logger;
        }

        public async Task OnBatchReceivedAsync(IAsyncEnumerable<SampleMessage> batch)
        {
            int sum = 0;
            int count = 0;

            await foreach (var message in batch)
            {
                sum += message.Number;
                count++;
            }

            _logger.LogInformation(
                "Received batch of {Count} message -> sum: {Sum}",
                count,
                sum);
        }
    }
}
