using Microsoft.Extensions.Logging;
using Silverback.Samples.Kafka.Basic.Common;

namespace Silverback.Samples.Kafka.Basic.Consumer
{
    public class SampleMessageSubscriber
    {
        private readonly ILogger<SampleMessageSubscriber> _logger;

        public SampleMessageSubscriber(ILogger<SampleMessageSubscriber> logger)
        {
            _logger = logger;
        }

        public void OnMessageReceived(SampleMessage message) =>
            _logger.LogInformation("Received {MessageNumber}", message.Number);
    }
}
