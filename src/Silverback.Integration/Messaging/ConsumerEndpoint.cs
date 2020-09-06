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
        /// <param name="name">
        ///     The endpoint name.
        /// </param>
        protected ConsumerEndpoint(string name)
            : base(name)
        {
        }

        /// <summary>
        ///     Gets or sets a value indicating whether an exception must be thrown if no subscriber is handling the
        ///     received message. The default is <c>false</c> and it means that the unhandled messages are silently
        ///     discarded.
        /// </summary>
        public bool ThrowIfUnhandled { get; set; }

        /// <inheritdoc cref="IConsumerEndpoint.GetUniqueConsumerGroupName" />
        public abstract string GetUniqueConsumerGroupName();
    }
}
