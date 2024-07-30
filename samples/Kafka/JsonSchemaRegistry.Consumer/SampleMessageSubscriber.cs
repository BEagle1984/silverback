using Microsoft.Extensions.Logging;
using Silverback.Samples.Kafka.JsonSchemaRegistry.Common;

namespace Silverback.Samples.Kafka.JsonSchemaRegistry.Consumer;

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
