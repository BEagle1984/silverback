// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Runtime.Serialization;

namespace Silverback.EventStore
{
    /// <summary>
    ///     The exception that is thrown when the event store detects that the record being saved has been
    ///     modified since it was read.
    /// </summary>
    [Serializable]
    public class EventStoreConcurrencyException : EventStoreException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="EventStoreConcurrencyException" /> class.
        /// </summary>
        public EventStoreConcurrencyException()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="EventStoreConcurrencyException" /> class with the
        ///     specified message.
        /// </summary>
        /// <param name="message">
        ///     The exception message.
        /// </param>
        public EventStoreConcurrencyException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="EventStoreConcurrencyException" /> class with the
        ///     specified message and inner exception.
        /// </summary>
        /// <param name="message">
        ///     The exception message.
        /// </param>
        /// <param name="innerException">
        ///     The inner exception.
        /// </param>
        public EventStoreConcurrencyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="EventStoreConcurrencyException" /> class with the
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
        protected EventStoreConcurrencyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
