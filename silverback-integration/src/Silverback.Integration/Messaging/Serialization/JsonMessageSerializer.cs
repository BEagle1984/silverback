using System;
using System.Text;
using Newtonsoft.Json;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization
{
    /// <summary>
    /// Serializes the message as JSON and then converts them to a UTF8 encoded byte array. 
    /// </summary>
    /// <seealso cref="Silverback.Messaging.Serialization.IMessageSerializer" />
    public class JsonMessageSerializer : IMessageSerializer
    {
        /// <summary>
        /// Serializes the specified message into a byte array.
        /// </summary>
        /// <param name="envelope">The envelope containing the message.</param>
        /// <returns></returns>
        public byte[] Serialize(IEnvelope envelope)
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));

            var json = JsonConvert.SerializeObject(envelope, typeof(IEnvelope), GetSerializerSettings());

            return Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        /// Deserializes the specified message from a byte array.
        /// </summary>
        /// <param name="message">The serialized message.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">message</exception>
        public IEnvelope Deserialize(byte[] message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var json = Encoding.UTF8.GetString(message);

            return JsonConvert.DeserializeObject<IEnvelope>(json, GetSerializerSettings());
        }

        private JsonSerializerSettings GetSerializerSettings()
            => new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto
            };
    }
}