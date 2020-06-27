// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging
{
    /// <inheritdoc cref="IConsumerEndpoint" />
    public abstract class ConsumerEndpoint : Endpoint, IConsumerEndpoint
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConsumerEndpoint" /> class.
        /// </summary>
        protected ConsumerEndpoint()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConsumerEndpoint" /> class.
        /// </summary>
        /// <param name="name">
        ///     The endpoint name.
        /// </param>
        protected ConsumerEndpoint(string name)
            : base(name)
        {
        }

        /// <inheritdoc cref="IConsumerEndpoint.ThrowIfUnhandled" />
        public bool ThrowIfUnhandled { get; set; }

        /// <inheritdoc cref="IConsumerEndpoint.GetUniqueConsumerGroupName" />
        public abstract string GetUniqueConsumerGroupName();
    }
}
