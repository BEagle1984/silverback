// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;

namespace Silverback.Messaging.Publishing
{
    /// <summary>
    ///     The exception that is thrown when some published messages aren't handled by any registered
    ///     subscriber.
    /// </summary>
    [Serializable]
    [ExcludeFromCodeCoverage]
    public class UnhandledMessageException : SilverbackException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UnhandledMessageException" /> class.
        /// </summary>
        public UnhandledMessageException()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UnhandledMessageException" /> class with the
        ///     specified message.
        /// </summary>
        /// <param name="message">
        ///     The exception message.
        /// </param>
        public UnhandledMessageException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UnhandledMessageException" /> class with the
        ///     specified message.
        /// </summary>
        /// <param name="unhandledMessage">
        ///     The message that wasn't handled.
        /// </param>
        public UnhandledMessageException(object unhandledMessage)
            : this(new[] { unhandledMessage })
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UnhandledMessageException" /> class with the
        ///     specified message.
        /// </summary>
        /// <param name="unhandledMessages">
        ///     The messages that weren't handled.
        /// </param>
        public UnhandledMessageException(IReadOnlyCollection<object> unhandledMessages)
            : base(GetErrorMessage(unhandledMessages))
        {
            UnhandledMessages = unhandledMessages;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UnhandledMessageException" /> class with the
        ///     specified message.
        /// </summary>
        /// <param name="unhandledMessages">
        ///     The messages that weren't handled.
        /// </param>
        /// <param name="message">
        ///     The exception message.
        /// </param>
        public UnhandledMessageException(IReadOnlyCollection<object> unhandledMessages, string message)
            : base(message)
        {
            UnhandledMessages = unhandledMessages;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UnhandledMessageException" /> class with the
        ///     specified message and inner exception.
        /// </summary>
        /// <param name="message">
        ///     The exception message.
        /// </param>
        /// <param name="innerException">
        ///     The inner exception.
        /// </param>
        public UnhandledMessageException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UnhandledMessageException" /> class with the
        ///     serialized data.
        /// </summary>
        /// <param name="info">
        ///     The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being
        ///     thrown.
        /// </param>
        /// <param name="context">
        ///     The <see cref="StreamingContext" /> that contains contextual information about the source or
        ///     destination.
        /// </param>
        protected UnhandledMessageException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        ///     Gets the messages that weren't handled.
        /// </summary>
        public IReadOnlyCollection<object>? UnhandledMessages { get; }

        private static string GetErrorMessage(IReadOnlyCollection<object> unhandledMessages) =>
            unhandledMessages.Count == 1
                ? $"No subscriber could be found to handle the message of type {unhandledMessages.First().GetType().FullName}."
                : "No subscriber could be found to handle some of the published messages " +
                  $"({string.Join(", ", unhandledMessages.Select(message => message.GetType().FullName).Distinct())}).";
    }
}
