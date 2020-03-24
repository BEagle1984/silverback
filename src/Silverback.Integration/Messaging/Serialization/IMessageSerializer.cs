// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization
{
    /// <summary>
    ///     Serializes and deserializes the messages sent through the broker.
    /// </summary>
    public interface IMessageSerializer
    {
        /// <summary>
        ///     Serializes the specified message object into a byte array.
        /// </summary>
        /// <param name="message">The message object to be serialized.</param>
        /// <param name="messageHeaders">The message headers collection.</param>
        /// <returns></returns>
        byte[] Serialize(object message, MessageHeaderCollection messageHeaders);

        /// <summary>
        ///     Deserializes the byte array back into a message object.
        /// </summary>
        /// <param name="message">The byte array to be deserialized.</param>
        /// <param name="messageHeaders">The message headers collection.</param>
        /// <returns></returns>
        object Deserialize(byte[] message, MessageHeaderCollection messageHeaders);
        
        /// <summary>
        ///     Serializes the specified message object into a byte array.
        /// </summary>
        /// <param name="message">The message object to be serialized.</param>
        /// <param name="messageHeaders">The message headers collection.</param>
        /// <returns></returns>
        Task<byte[]> SerializeAsync(object message, MessageHeaderCollection messageHeaders);

        /// <summary>
        ///     Deserializes the byte array back into a message object.
        /// </summary>
        /// <param name="message">The byte array to be deserialized.</param>
        /// <param name="messageHeaders">The message headers collection.</param>
        /// <returns></returns>
        Task<object> DeserializeAsync(byte[] message, MessageHeaderCollection messageHeaders);
    }
}