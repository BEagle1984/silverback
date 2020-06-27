// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.Performance.LegacyImplementations
{
    /// <summary>
    ///     Serializes and deserializes the messages of type <typeparamref name="TMessage" /> in JSON format.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be serialized and/or deserialized.
    /// </typeparam>
    public class JsonMessageSerializer<TMessage> : Tests.Performance.LegacyImplementations.JsonMessageSerializer
    {
        private readonly Type _type = typeof(TMessage);

        /// <inheritdoc cref="Tests.Performance.LegacyImplementations.JsonMessageSerializer.Serialize" />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public override byte[]? Serialize(
            object? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            if (message == null)
                return null;

            if (message is byte[] bytes)
                return bytes;

            var jsonString = JsonConvert.SerializeObject(message, _type, Settings);

            return GetSystemEncoding().GetBytes(jsonString);
        }

        /// <inheritdoc cref="Tests.Performance.LegacyImplementations.JsonMessageSerializer.Deserialize" />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public override (object?, Type) Deserialize(
            byte[]? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            if (message == null || message.Length == 0)
                return (null, _type);

            var json = GetSystemEncoding().GetString(message);

            var deserializedObject = JsonConvert.DeserializeObject(json, _type, Settings);
            return (deserializedObject, _type);
        }
    }
}
