// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Silverback.Messaging.Serialization
{
    public class JsonMessageSerializer<TMessage> : IMessageSerializer
    {
        [DefaultValue("UTF8")] public MessageEncoding Encoding { get; set; } = MessageEncoding.UTF8;

        public JsonSerializerSettings Settings { get; } = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto
        };

        public byte[] Serialize(object message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            if (message is byte[] bytes)
                return bytes;

            var json = JsonConvert.SerializeObject(message, typeof(TMessage), Settings);

            return GetEncoding().GetBytes(json);
        }

        public object Deserialize(byte[] message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var json = GetEncoding().GetString(message);

            return JsonConvert.DeserializeObject<TMessage>(json, Settings);
        }

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

    public class JsonMessageSerializer : JsonMessageSerializer<object>
    {
    }
}
