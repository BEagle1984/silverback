// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Subscribers
{
    /// <summary>
    /// Used to identify the methods that have to be subscribed to the messages stream.
    /// The decorated method must have a single input parameter of type <see cref="IMessage"/>
    /// or derived type. The methods can be async (returning a Task).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SubscribeAttribute : Attribute
    {
        private int _maxDegreeOfParallelism = int.MaxValue;

        /// <summary>
        /// A value indicating whether the method can be executed concurrently to other
        /// methods handling the <b>same message</b>.
        /// The default value is <c>true</c> (the method will be executed sequentially
        /// to other subscribers).
        /// </summary>
        public bool Exclusive { get; set; } = true;

        /// <summary>
        /// A value indicating whether the method can be executed concurrently when
        /// multiple messages are fired at the same time (e.g. in a batch).
        /// The default value is <c>false</c> (the messages are processed sequentially).
        /// </summary>
        public bool Parallel { get; set; } = false;

        /// <summary>
        /// Limit the number of messages that are processed concurrently.
        /// Used only together with Parallel = true.
        /// The default value is Int32.Max and means that there is no limit to the
        /// degree of parallelism.
        /// </summary>
        public int MaxDegreeOfParallelism
        {
            get => _maxDegreeOfParallelism;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        "MaxDegreeOfParallelism must be greater or equal to 1.");

                _maxDegreeOfParallelism = value;
            }
        }

        public int? GetMaxDegreeOfParallelism() =>
            _maxDegreeOfParallelism != int.MaxValue ? _maxDegreeOfParallelism : (int?)null;
    }
}