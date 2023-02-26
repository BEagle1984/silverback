using Silverback.Messaging.Messages;

namespace Silverback.Samples.Mqtt.BinaryFileStreaming.Producer.Messages;

public class CustomBinaryFileMessage : BinaryMessage
{
    [Header("x-filename")]
    public string? Filename { get; set; }
}
