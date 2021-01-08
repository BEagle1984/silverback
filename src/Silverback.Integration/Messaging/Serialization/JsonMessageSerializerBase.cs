// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization
{
    /// <summary>
    ///     The base class for <see cref="JsonMessageSerializer" /> and
    ///     <see cref="JsonMessageSerializer{TMessage}" />.
    /// </summary>
    public abstract class JsonMessageSerializerBase : IMessageSerializer
    {
        /// <summary>
        ///     Gets or sets the options to be passed to the <see cref="JsonSerializer" />.
        /// </summary>
        public JsonSerializerOptions Options { get; set; } = new();

        /// <inheritdoc cref="IMessageSerializer.RequireHeaders" />
        public abstract bool RequireHeaders { get; }

        /// <inheritdoc cref="IMessageSerializer.SerializeAsync" />
        public abstract ValueTask<Stream?> SerializeAsync(
            object? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context);

        /// <inheritdoc cref="IMessageSerializer.DeserializeAsync" />
        public abstract ValueTask<(object? Message, Type MessageType)> DeserializeAsync(
            Stream? messageStream,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context);
    }
}
