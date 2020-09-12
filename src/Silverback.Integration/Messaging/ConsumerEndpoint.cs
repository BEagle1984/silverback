// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Batch;
using Silverback.Messaging.Inbound.ErrorHandling;

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
        ///     Gets or sets the error policy to be applied when an exception occurs during the processing of the
        ///     consumed messages.
        /// </summary>
        public IErrorPolicy? ErrorPolicy { get; set; }

        /// <summary>
        ///     Gets or sets the batch settings. Can be used to enable and setup batch processing.
        /// </summary>
        public BatchSettings? Batch { get; set; }

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
