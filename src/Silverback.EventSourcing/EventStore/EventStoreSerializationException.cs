// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Runtime.Serialization;

namespace Silverback.EventStore
{
    /// <summary>
    ///     The exception that is thrown by the <see cref="EventSerializer" /> when the event cannot be
    ///     serialized or deserialized. This exception is thrown only when a Silverback specific error occurs,
    ///     other exceptions related to reflection or the underlying serializer are not wrapped.
    /// </summary>
    [Serializable]
    public class EventStoreSerializationException : EventStoreException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="EventStoreSerializationException" /> class.
        /// </summary>
        public EventStoreSerializationException()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="EventStoreSerializationException" /> class with the
        ///     specified message.
        /// </summary>
        /// <param name="message">
        ///     The exception message.
        /// </param>
        public EventStoreSerializationException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="EventStoreSerializationException" /> class with the
        ///     specified message and inner exception.
        /// </summary>
        /// <param name="message">
        ///     The exception message.
        /// </param>
        /// <param name="innerException">
        ///     The inner exception.
        /// </param>
        public EventStoreSerializationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="EventStoreSerializationException" /> class with the
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
        protected EventStoreSerializationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
