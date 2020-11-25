// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Silverback.EventStore
{
    /// <summary>
    ///     The base class for the exceptions related to the event store.
    /// </summary>
    [Serializable]
    [ExcludeFromCodeCoverage]
    public abstract class EventStoreException : SilverbackException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="EventStoreException" /> class.
        /// </summary>
        protected EventStoreException()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="EventStoreException" /> class with the specified
        ///     message.
        /// </summary>
        /// <param name="message">
        ///     The exception message.
        /// </param>
        protected EventStoreException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="EventStoreException" /> class with the specified
        ///     message and inner exception.
        /// </summary>
        /// <param name="message">
        ///     The exception message.
        /// </param>
        /// <param name="innerException">
        ///     The inner exception.
        /// </param>
        protected EventStoreException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="EventStoreException" /> class with the serialized data.
        /// </summary>
        /// <param name="info">
        ///     The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being
        ///     thrown.
        /// </param>
        /// <param name="context">
        ///     The <see cref="StreamingContext" /> that contains contextual information about the source or
        ///     destination.
        /// </param>
        protected EventStoreException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
