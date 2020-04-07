// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization
{
    /// <summary>
    ///     The default implementation of a <see cref="IKafkaMessageSerializer" /> simply uses
    ///     the provided <see cref="IMessageSerializer" /> for the value and treats the key as a
    ///     UTF-8 encoded string.
    /// </summary>
    /// <inheritdoc cref="IKafkaMessageSerializer" />
    public class DefaultKafkaMessageSerializer : IKafkaMessageSerializer
    {
        private readonly IMessageSerializer _serializer;

        public DefaultKafkaMessageSerializer(IMessageSerializer serializer)
        {
            _serializer = serializer;
        }

        public byte[] Serialize(
            object message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            _serializer.Serialize(message, messageHeaders, context);

        public object Deserialize(
            byte[] message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            _serializer.Deserialize(message, messageHeaders, context);

        public Task<byte[]> SerializeAsync(
            object message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            _serializer.SerializeAsync(message, messageHeaders, context);

        public Task<object> DeserializeAsync(
            byte[] message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            _serializer.DeserializeAsync(message, messageHeaders, context);

        public byte[] SerializeKey(
            string key,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            Encoding.UTF8.GetBytes(key);

        public string DeserializeKey(
            byte[] key,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            Encoding.UTF8.GetString(key);
    }
}