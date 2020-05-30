// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.BinaryFiles
{
    /// <summary>
    ///     Handles the <see cref="IBinaryFileMessage" />. It's not really a serializer, since the raw binary
    ///     content is transmitted as-is.
    /// </summary>
    /// <typeparam name="TModel">
    ///     The type of the <see cref="IBinaryFileMessage" /> implementation.
    /// </typeparam>
    public class BinaryFileMessageSerializer<TModel> : BinaryFileMessageSerializer
        where TModel : IBinaryFileMessage, new()
    {
        private readonly Type _type = typeof(TModel);

        /// <inheritdoc cref="BinaryFileMessageSerializer.Serialize" />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public override byte[]? Serialize(
            object? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            Check.NotNull(messageHeaders, nameof(messageHeaders));

            if (message == null)
                return null;

            if (message is byte[] bytes)
                return bytes;

            var binaryFileMessage = message as IBinaryFileMessage;
            if (binaryFileMessage == null)
            {
                throw new ArgumentException(
                    "The message is not implementing the IBinaryFileMessage interface.",
                    nameof(message));
            }

            return binaryFileMessage.Content;
        }

        /// <inheritdoc cref="BinaryFileMessageSerializer.Deserialize" />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public override (object?, Type) Deserialize(
            byte[]? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            var binaryFileMessage = new TModel { Content = message };

            return (binaryFileMessage, _type);
        }
    }
}
