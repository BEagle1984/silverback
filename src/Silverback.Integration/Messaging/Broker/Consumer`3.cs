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
        /// <param name="behaviorsProvider">
        ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
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
            IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider,
            ISilverbackIntegrationLogger<Consumer<TBroker, TEndpoint, TOffset>> logger)
            : base(broker, endpoint, behaviorsProvider, serviceProvider, logger)
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

        /// <inheritdoc cref="Consumer.CommitAsync(System.Collections.Generic.IReadOnlyCollection{Silverback.Messaging.Broker.IOffset})" />
        public override Task CommitAsync(IReadOnlyCollection<IOffset> offsets) =>
            CommitCoreAsync(offsets.Cast<TOffset>().ToList());

        /// <inheritdoc cref="Consumer.RollbackAsync(System.Collections.Generic.IReadOnlyCollection{Silverback.Messaging.Broker.IOffset})" />
        public override Task RollbackAsync(IReadOnlyCollection<IOffset> offsets) =>
            RollbackCoreAsync(offsets.Cast<TOffset>().ToList());

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
        protected abstract Task CommitCoreAsync(IReadOnlyCollection<TOffset> offsets);

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
        protected abstract Task RollbackCoreAsync(IReadOnlyCollection<TOffset> offsets);
    }
}
