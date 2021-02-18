using Microsoft.Extensions.Logging;
using Silverback.Samples.Mqtt.Basic.Common;

namespace Silverback.Samples.Mqtt.Basic.ConsumerV3
{
    public class SampleMessageSubscriber
    {
        private readonly ILogger<SampleMessageSubscriber> _logger;

        public SampleMessageSubscriber(ILogger<SampleMessageSubscriber> logger)
        {
            _logger = logger;
        }

        public void OnMessageReceived(SampleMessage message) =>
            _logger.LogInformation($"Received {message.Number}");
    }
}
