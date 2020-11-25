using Silverback.Messaging.Messages;

namespace Silverback.Samples.Kafka.BinaryFileStreaming.Consumer.Messages
{
    public class CustomBinaryFileMessage : BinaryFileMessage
    {
        [Header("x-filename")]
        public string? Filename { get; set; }
    }
}