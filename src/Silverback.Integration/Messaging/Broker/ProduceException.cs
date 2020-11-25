// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     The exception that is thrown by the <see cref="IProducer" /> when the message cannot be produced or
    ///     the message broker didn't acknowledge it.
    /// </summary>
    [Serializable]
    [ExcludeFromCodeCoverage]
    public class ProduceException : SilverbackException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ProduceException" /> class.
        /// </summary>
        public ProduceException()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ProduceException" /> class with the specified message.
        /// </summary>
        /// <param name="message">
        ///     The exception message.
        /// </param>
        public ProduceException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ProduceException" /> class with the specified message
        ///     and inner exception.
        /// </summary>
        /// <param name="message">
        ///     The exception message.
        /// </param>
        /// <param name="innerException">
        ///     The inner exception.
        /// </param>
        public ProduceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ProduceException" /> class with the serialized data.
        /// </summary>
        /// <param name="info">
        ///     The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being
        ///     thrown.
        /// </param>
        /// <param name="context">
        ///     The <see cref="StreamingContext" /> that contains contextual information about the source or
        ///     destination.
        /// </param>
        protected ProduceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
