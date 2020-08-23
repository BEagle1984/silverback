// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Consumer" />
    [SuppressMessage("", "CA1005", Justification = Justifications.NoWayToReduceTypeParameters)]
    public abstract class Consumer<TBroker, TEndpoint, TOffset> : Consumer
        where TBroker : IBroker
        where TEndpoint : IConsumerEndpoint
        where TOffset : IOffset
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Consumer{TBroker, TEndpoint, TOffset}" /> class.
        /// </summary>
        /// <param name="broker">
        ///     The <see cref="IBroker" /> that is instantiating the consumer.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint to be consumed.
        /// </param>
        /// <param name="callback">
        ///     The delegate to be invoked when a message is received.
        /// </param>
        /// <param name="behaviors">
        ///     The behaviors to be added to the pipeline.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackIntegrationLogger" />.
        /// </param>
        protected Consumer(
            TBroker broker,
            TEndpoint endpoint,
            MessagesReceivedAsyncCallback callback,
            IReadOnlyList<IConsumerBehavior>? behaviors,
            IServiceProvider serviceProvider,
            ISilverbackIntegrationLogger<Consumer<TBroker, TEndpoint, TOffset>> logger)
            : base(broker, endpoint, callback, behaviors, serviceProvider, logger)
        {
        }

        /// <summary>
        ///     Gets the <see cref="IBroker" /> that owns this consumer.
        /// </summary>
        protected new TBroker Broker => (TBroker)base.Broker;

        /// <summary>
        ///     Gets the <see cref="IConsumerEndpoint" /> representing the endpoint that is being consumed.
        /// </summary>
        protected new TEndpoint Endpoint => (TEndpoint)base.Endpoint;

        /// <inheritdoc cref="Consumer.Commit(IReadOnlyCollection{IOffset})" />
        public override Task Commit(IReadOnlyCollection<IOffset> offsets) =>
            CommitCore(offsets.Cast<TOffset>().ToList());

        /// <inheritdoc cref="Consumer.Rollback(IReadOnlyCollection{IOffset})" />
        public override Task Rollback(IReadOnlyCollection<IOffset> offsets) =>
            RollbackCore(offsets.Cast<TOffset>().ToList());

        /// <summary>
        ///     <param>
        ///         Confirms that the messages at the specified offsets have been successfully processed.
        ///     </param>
        ///     <param>
        ///         The acknowledgement will be sent to the message broker and the message will never be processed
        ///         again (by the same logical consumer / consumer group).
        ///     </param>
        /// </summary>
        /// <param name="offsets">
        ///     The offsets to be committed.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected abstract Task CommitCore(IReadOnlyCollection<TOffset> offsets);

        /// <summary>
        ///     <param>
        ///         Notifies that an error occured while processing the messages at the specified offsets.
        ///     </param>
        ///     <param>
        ///         If necessary the information will be sent to the message broker to ensure that the message will
        ///         be re-processed.
        ///     </param>
        /// </summary>
        /// <param name="offsets">
        ///     The offsets to be rolled back.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected abstract Task RollbackCore(IReadOnlyCollection<TOffset> offsets);
    }
}
