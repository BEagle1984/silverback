// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories.Model
{
    /// <summary>
    ///     Encapsulates the information related to a message stored in the outbound queue.
    /// </summary>
    public class QueuedMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="QueuedMessage" /> class.
        /// </summary>
        /// <param name="messageType">
        ///    The type of the message.
        /// </param>
        /// <param name="content">
        ///     The message raw binary content (body).
        /// </param>
        /// <param name="headers">
        ///     The message headers.
        /// </param>
        /// <param name="endpointName">
        ///     The name of the target endpoint.
        /// </param>
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public QueuedMessage(
            Type? messageType,
            byte[]? content,
            IEnumerable<MessageHeader>? headers,
            string endpointName)
        {
            MessageType = messageType;
            Content = content;
            Headers = headers?.ToList();
            EndpointName = endpointName;
        }

        /// <summary>
        ///     Gets or sets the type of the message.
        /// </summary>
        public Type? MessageType { get; set; }

        /// <summary>
        ///     Gets the message raw binary content (body).
        /// </summary>
        [SuppressMessage("", "CA1819", Justification = Justifications.CanExposeByteArray)]
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public byte[]? Content { get; }

        /// <summary>
        ///     Gets the message headers.
        /// </summary>
        public IReadOnlyCollection<MessageHeader>? Headers { get; }

        /// <summary>
        ///     Gets the name of the target endpoint.
        /// </summary>
        public string EndpointName { get; }
    }
}
