using Silverback.Messaging.Messages;

namespace Silverback.Samples.Kafka.BinaryFileStreaming.Producer.Messages
{
    public class CustomBinaryFileMessage : BinaryFileMessage
    {
        [Header("x-filename")]
        public string? Filename { get; set; }
    }
}
