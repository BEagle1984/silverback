// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel;
using Newtonsoft.Json;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization
{
    // TODO: Test separately
    public class JsonMessageSerializer<TMessage> : JsonMessageSerializer
    {
        public override byte[] Serialize(object message, MessageHeaderCollection messageHeaders)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            if (message is byte[] bytes)
                return bytes;

            var type = typeof(TMessage);
            var json = JsonConvert.SerializeObject(message, type, Settings);

            return GetEncoding().GetBytes(json);
        }

        public override object Deserialize(byte[] message, MessageHeaderCollection messageHeaders)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var type = typeof(TMessage);
            var json = GetEncoding().GetString(message);
            
            return JsonConvert.DeserializeObject(json, type, Settings);
        }
    }

    public class JsonMessageSerializer : IMessageSerializer
    {
        [DefaultValue("UTF8")] public MessageEncoding Encoding { get; set; } = MessageEncoding.UTF8;

        public JsonSerializerSettings Settings { get; } = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.None
        };

        public virtual byte[] Serialize(object message, MessageHeaderCollection messageHeaders)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (messageHeaders == null) throw new ArgumentNullException(nameof(messageHeaders));

            if (message is byte[] bytes)
                return bytes;

            var type = message.GetType();
            var json = JsonConvert.SerializeObject(message, type, Settings);

            messageHeaders.AddOrReplace(MessageHeader.MessageTypeKey, type.AssemblyQualifiedName);

            return GetEncoding().GetBytes(json);
        }

        public virtual object Deserialize(byte[] message, MessageHeaderCollection messageHeaders)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (messageHeaders == null) throw new ArgumentNullException(nameof(messageHeaders));

            var typeName = messageHeaders.GetValue(MessageHeader.MessageTypeKey) ?? throw new SilverbackException($"Message type header not found ('{MessageHeader.MessageTypeKey}').");
            var type = Type.GetType(typeName);
            var json = GetEncoding().GetString(message);

            return JsonConvert.DeserializeObject(json, type, Settings);
        }

        protected System.Text.Encoding GetEncoding()
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
