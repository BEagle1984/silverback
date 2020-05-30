// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Subscribers.Subscriptions
{
    /// <summary>
    ///     The subscription options such as the parallelism settings.
    /// </summary>
    public class SubscriptionOptions
    {
        private int? _maxDegreeOfParallelism;

        /// <summary>
        ///     Gets or sets a value indicating whether the method can be executed concurrently to other methods
        ///     handling the <b>
        ///         same message
        ///     </b>. The default value is <c>true</c> (the method will be executed sequentially to other subscribers).
        /// </summary>
        public bool Exclusive { get; set; } = true;

        /// <summary>
        ///     Gets or sets a value indicating whether the method can be executed concurrently when multiple
        ///     messages are fired at the same time (e.g. in a batch). The default value is <c>false</c> (the messages are processed sequentially).
        /// </summary>
        public bool Parallel { get; set; } = false;

        /// <summary>
        ///     Gets or sets the maximum number of messages that are processed concurrently. Used only together with
        ///     Parallel = true. The default value is null and means that there is no limit to the degree of
        ///     parallelism.
        /// </summary>
        public int? MaxDegreeOfParallelism
        {
            get => _maxDegreeOfParallelism;
            set
            {
                if (value != null && value <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        "MaxDegreeOfParallelism must be greater or equal to 1 (or null).");
                }

                _maxDegreeOfParallelism = value;
            }
        }
    }
}
