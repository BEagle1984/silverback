using System;
using System.ComponentModel;
using Newtonsoft.Json;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization
{
    public class JsonMessageSerializer : IMessageSerializer
    {
        public byte[] Serialize(IMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var json = JsonConvert.SerializeObject(message, typeof(IMessage), Settings);

            return GetEncoding().GetBytes(json);
        }

        public IMessage Deserialize(byte[] message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var json = GetEncoding().GetString(message);

            return JsonConvert.DeserializeObject<IMessage>(json, Settings);
        }

        [DefaultValue("UTF8")]
        public MessageEncoding Encoding { get; set; } = MessageEncoding.UTF8;

        public JsonSerializerSettings Settings { get; } = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto
        };

        private System.Text.Encoding GetEncoding()
        {
            switch (Encoding)
            {
                case MessageEncoding.Default:
                    return System.Text.Encoding.Default;
                case MessageEncoding.ASCII:
                    return System.Text.Encoding.ASCII;
                case MessageEncoding.UTF8:
                    return System.Text.Encoding.UTF8;
                case MessageEncoding.UTF32:
                    return System.Text.Encoding.UTF32;
                case MessageEncoding.Unicode:
                    return System.Text.Encoding.Unicode;
                default:
                    throw new InvalidOperationException("Unhandled encoding.");
            }
        }
    }
}