using Silverback.Messaging.Messages;

namespace Silverback.Samples.Mqtt.BinaryFileStreaming.Producer.Messages;

public class CustomBinaryMessage : BinaryMessage
{
    [Header("x-filename")]
    public string? Filename { get; set; }
}
