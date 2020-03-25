// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization
{
    /// <summary>
    ///     The default implementation of a <see cref="IKafkaMessageSerializer"/> simply uses
    ///     the provided <see cref="IMessageSerializer"/> for the value and treats the key as a
    ///     UTF-8 encoded string.
    /// </summary>
    public class DefaultKafkaMessageSerializer : IKafkaMessageSerializer
    {
        private readonly IMessageSerializer _serializer;

        public DefaultKafkaMessageSerializer(IMessageSerializer serializer)
        {
            _serializer = serializer;
        }

        /// <inheritdoc cref="IKafkaMessageSerializer"/>
        public byte[] Serialize(object message, MessageHeaderCollection messageHeaders) =>
            _serializer.Serialize(message, messageHeaders);

        /// <inheritdoc cref="IKafkaMessageSerializer"/>
        public object Deserialize(byte[] message, MessageHeaderCollection messageHeaders) =>
            _serializer.Deserialize(message, messageHeaders);

        /// <inheritdoc cref="IMessageSerializer"/>
        public Task<byte[]> SerializeAsync(object message, MessageHeaderCollection messageHeaders) =>
            _serializer.SerializeAsync(message, messageHeaders);

        /// <inheritdoc cref="IMessageSerializer"/>
        public Task<object> DeserializeAsync(byte[] message, MessageHeaderCollection messageHeaders) => 
            _serializer.DeserializeAsync(message, messageHeaders);

        /// <inheritdoc cref="IKafkaMessageSerializer"/>
        public byte[] SerializeKey(string key, MessageHeaderCollection messageHeaders) =>
            Encoding.UTF8.GetBytes(key);

        /// <inheritdoc cref="IKafkaMessageSerializer"/>
        public string DeserializeKey(byte[] key, MessageHeaderCollection messageHeaders) =>
            Encoding.UTF8.GetString(key);
    }
}