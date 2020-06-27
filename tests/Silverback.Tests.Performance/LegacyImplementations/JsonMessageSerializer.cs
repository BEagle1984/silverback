// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Tests.Performance.LegacyImplementations
{
    /// <summary>
    ///     Serializes the messages in JSON format and relies on some added headers to determine the message
    ///     type upon deserialization. This default serializer is ideal when the producer and the consumer are
    ///     both using Silverback.
    /// </summary>
    public class JsonMessageSerializer : IMessageSerializer
    {
        /// <summary>
        ///     Gets the default static instance of <see cref="JsonMessageSerializer" />.
        /// </summary>
        public static JsonMessageSerializer Default { get; } = new JsonMessageSerializer();

        /// <summary>
        ///     Gets or sets the message encoding. The default is UTF8.
        /// </summary>
        [DefaultValue("UTF8")]
        public MessageEncoding Encoding { get; set; } = MessageEncoding.UTF8;

        /// <summary>
        ///     Gets or sets the settings to be applied to the Json.NET serializer.
        /// </summary>
        public JsonSerializerSettings Settings { get; set; } = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto
        };

        /// <inheritdoc cref="IMessageSerializer.Serialize" />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public virtual byte[]? Serialize(
            object? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            Check.NotNull(messageHeaders, nameof(messageHeaders));

            if (message == null)
                return null;

            if (message is byte[] bytes)
                return bytes;

            var type = message.GetType();
            var json = JsonConvert.SerializeObject(message, type, Settings);

            messageHeaders.AddOrReplace(DefaultMessageHeaders.MessageType, type.AssemblyQualifiedName);

            return GetSystemEncoding().GetBytes(json);
        }

        /// <inheritdoc cref="IMessageSerializer.Deserialize" />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public virtual (object?, Type) Deserialize(
            byte[]? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            Check.NotNull(messageHeaders, nameof(messageHeaders));

            var type = SerializationHelper.GetTypeFromHeaders(messageHeaders);

            if (message == null || message.Length == 0)
                return (null, type);

            var jsonString = GetSystemEncoding().GetString(message);

            var deserializedObject = JsonConvert.DeserializeObject(jsonString, type, Settings) ??
                                     throw new MessageSerializerException("The deserialization returned null.");

            return (deserializedObject, type);
        }

        /// <inheritdoc cref="IMessageSerializer.SerializeAsync" />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public virtual Task<byte[]?> SerializeAsync(
            object? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            Task.FromResult(Serialize(message, messageHeaders, context));

        /// <inheritdoc cref="IMessageSerializer.DeserializeAsync" />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public virtual Task<(object?, Type)> DeserializeAsync(
            byte[]? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            Task.FromResult(Deserialize(message, messageHeaders, context));

        /// <summary>
        ///     Maps the <see cref="MessageEncoding" /> to the <see cref="System.Text.Encoding" />.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.Text.Encoding" /> that matches the current <see cref="MessageEncoding" />.
        /// </returns>
        protected Encoding GetSystemEncoding() =>
            Encoding switch
            {
                MessageEncoding.Default => System.Text.Encoding.Default,
                MessageEncoding.ASCII => System.Text.Encoding.ASCII,
                MessageEncoding.UTF8 => System.Text.Encoding.UTF8,
                MessageEncoding.UTF32 => System.Text.Encoding.UTF32,
                MessageEncoding.Unicode => System.Text.Encoding.Unicode,
                _ => throw new InvalidOperationException("Unhandled encoding.")
            };
    }
}
