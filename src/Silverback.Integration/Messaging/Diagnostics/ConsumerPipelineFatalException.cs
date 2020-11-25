﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Runtime.Serialization;

namespace Silverback.Messaging.Diagnostics
{
    /// <summary>
    ///     The exception that is rethrown by the <see cref="FatalExceptionLoggerConsumerBehavior" /> when a fatal
    ///     exception occurs down the consumer pipeline.
    /// </summary>
    [Serializable]
    public class ConsumerPipelineFatalException : SilverbackException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConsumerPipelineFatalException" /> class.
        /// </summary>
        public ConsumerPipelineFatalException()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConsumerPipelineFatalException" /> class with the
        ///     specified message.
        /// </summary>
        /// <param name="message">
        ///     The exception message.
        /// </param>
        public ConsumerPipelineFatalException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConsumerPipelineFatalException" /> class with the
        ///     specified message and inner exception.
        /// </summary>
        /// <param name="message">
        ///     The exception message.
        /// </param>
        /// <param name="innerException">
        ///     The inner exception.
        /// </param>
        public ConsumerPipelineFatalException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConsumerPipelineFatalException" /> class with the
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
        protected ConsumerPipelineFatalException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
