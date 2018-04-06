using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization
{
    /// <summary>
    /// Serializes and deserializes the messages sent through the broker.
    /// </summary>
    public interface IMessageSerializer
    {
        /// <summary>
        /// Serializes the specified message into a byte array.
        /// </summary>
        /// <param name="envelope">The envelope containing the message.</param>
        /// <returns></returns>
        byte[] Serialize(IEnvelope envelope);

        /// <summary>
        /// Deserializes the specified message from a byte array.
        /// </summary>
        /// <param name="message">The serialized message.</param>
        /// <returns></returns>
        IEnvelope Deserialize(byte[] message);
    }
}