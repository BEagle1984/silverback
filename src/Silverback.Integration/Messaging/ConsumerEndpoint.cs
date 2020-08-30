// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Inbound.ExactlyOnce;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Batch;

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
        public IErrorPolicy ErrorPolicy { get; set; } = Configuration.ErrorPolicy.Stop();

        /// <summary>
        ///     Gets or sets the strategy to be used to guarantee that each message is consumed only once.
        /// </summary>
        public IExactlyOnceStrategy? ExactlyOnceStrategy { get; set; }

        /// <summary>
        ///     Gets or sets the batch settings. Can be used to enable and setup batch processing.
        /// </summary>
        public BatchSettings? Batch { get; set; }

        /// <summary>
        ///     Gets or sets the sequence settings. A sequence is a set of related messages, like the chunks belonging
        ///     to the same message or the messages in a dataset.
        /// </summary>
        public SequenceSettings Sequence { get; set; } = new SequenceSettings();

        /// <summary>
        ///     Gets or sets a value indicating whether an exception must be thrown if no subscriber is handling the
        ///     received message. The default is <c>true</c>.
        /// </summary>
        public bool ThrowIfUnhandled { get; set; } = true;

        /// <inheritdoc cref="IConsumerEndpoint.GetUniqueConsumerGroupName" />
        public abstract string GetUniqueConsumerGroupName();

        /// <inheritdoc cref="IEndpoint.Validate" />
        public override void Validate()
        {
            base.Validate();

            if (Sequence == null)
                throw new EndpointConfigurationException("Sequence cannot be null.");

            if (ErrorPolicy == null)
                throw new EndpointConfigurationException("ErrorPolicy cannot be null.");

            Batch?.Validate();
        }
    }
}
