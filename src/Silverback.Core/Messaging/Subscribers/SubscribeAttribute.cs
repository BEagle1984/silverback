// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Subscribers
{
    /// <summary>
    ///     Used to identify the methods that have to be subscribed to the messages stream. The first parameter
    ///     of the subscriber method always correspond to the message and must be declared with a type
    ///     compatible with the message to be received (the message type, a base type or an implemented
    ///     interface) or a collection of items of that type. The methods can be either synchronous or
    ///     asynchronous (returning a <see cref="Task" />) and don't need to be publicly visible.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SubscribeAttribute : Attribute
    {
        private int _maxDegreeOfParallelism = int.MaxValue;

        /// <summary>
        ///     Gets or sets a value indicating whether the method can be executed concurrently to other methods
        ///     handling the <b>same message</b>. The default value is <c>true</c> (the method will be executed
        ///     sequentially to other subscribers), unless an <see cref="IMessageStreamEnumerable{TMessage}" /> is
        ///     being subscribed, in which case this parameter is in fact ignored and always considered <c>false</c>.
        /// </summary>
        /// <remarks>
        ///     This setting is ignored when subscribing to an <see cref="IMessageStreamEnumerable{TMessage}" />.
        /// </remarks>
        public bool Exclusive { get; set; } = true;

        /// <summary>
        ///     <para>
        ///         Gets or sets a value indicating whether the method can be executed concurrently when multiple
        ///         messages are published at the same time (with a single call to the <c>Publish</c> method.
        ///         The default value is <c>false</c> (the messages are processed sequentially).
        ///     </para>
        ///     <para>
        ///         Note that this setting doesn't apply to the messages consumed from a message broker. Refer to the
        ///         endpoint settings to control the behavior of the consumer.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     This setting is ignored when subscribing to an enumerable, a collection or an observable of messages.
        /// </remarks>
        public bool Parallel { get; set; }

        /// <summary>
        ///     Gets or sets the maximum number of messages that are processed concurrently. Used only together with
        ///     <c>Parallel = true</c>. The default value is <c>Int32.MaxValue</c> and means that there is no limit to
        ///     the degree of parallelism.
        /// </summary>
        /// <remarks>
        ///     This setting is ignored when subscribing to an enumerable, a collection or an observable of messages.
        /// </remarks>
        public int MaxDegreeOfParallelism
        {
            get => _maxDegreeOfParallelism;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        "MaxDegreeOfParallelism must be greater or equal to 1.");
                }

                _maxDegreeOfParallelism = value;
            }
        }
    }
}
