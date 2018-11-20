using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization
{
    /// <summary>
    /// Serializes and deserializes the messages sent through the broker.
    /// </summary>
    public interface IMessageSerializer
    {
        byte[] Serialize(IEnvelope envelope);

        IEnvelope Deserialize(byte[] message);
    }
}