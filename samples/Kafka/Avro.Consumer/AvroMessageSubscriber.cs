using Microsoft.Extensions.Logging;
using Silverback.Examples.Messages;

namespace Silverback.Samples.Kafka.Avro.Consumer;

public class AvroMessageSubscriber
{
    private readonly ILogger<AvroMessageSubscriber> _logger;

    public AvroMessageSubscriber(ILogger<AvroMessageSubscriber> logger)
    {
        _logger = logger;
    }

    public void OnMessageReceived(AvroMessage message) =>
        _logger.LogInformation("Received {MessageNumber}", message.number);
}
