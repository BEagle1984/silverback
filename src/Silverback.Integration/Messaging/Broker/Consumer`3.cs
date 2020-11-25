// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Sequences;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Consumer" />
    [SuppressMessage("", "CA1005", Justification = Justifications.NoWayToReduceTypeParameters)]
    public abstract class Consumer<TBroker, TEndpoint, TOffset> : Consumer
        where TBroker : IBroker
        where TEndpoint : IConsumerEndpoint
        where TOffset : IOffset
    {
        private readonly ISilverbackIntegrationLogger<Consumer<TBroker, TEndpoint, TOffset>> _logger;

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
            _logger = logger;
        }

        /// <summary>
        ///     Gets the <see cref="IBroker" /> that owns this consumer.
        /// </summary>
        public new TBroker Broker => (TBroker)base.Broker;

        /// <summary>
        ///     Gets the <see cref="IConsumerEndpoint" /> representing the endpoint that is being consumed.
        /// </summary>
        public new TEndpoint Endpoint => (TEndpoint)base.Endpoint;

        /// <inheritdoc cref="Consumer.CommitAsync(System.Collections.Generic.IReadOnlyCollection{Silverback.Messaging.Broker.IOffset})" />
        public override Task CommitAsync(IReadOnlyCollection<IOffset> offsets)
        {
            try
            {
                return CommitCoreAsync(offsets.Cast<TOffset>().ToList());
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    IntegrationEventIds.ConsumerCommitError,
                    exception,
                    "Error occurred during commit. (offsets: {offsets})",
                    string.Join(", ", offsets.Select(offset => offset.Value)));
                throw;
            }
        }

        /// <inheritdoc cref="Consumer.RollbackAsync(IReadOnlyCollection{IOffset})" />
        public override async Task RollbackAsync(IReadOnlyCollection<IOffset> offsets)
        {
            try
            {
                await RollbackCoreAsync(offsets.Cast<TOffset>().ToList()).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    IntegrationEventIds.ConsumerRollbackError,
                    exception,
                    "Error occurred during rollback. (offsets: {offsets})",
                    string.Join(", ", offsets.Select(offset => offset.Value)));
                throw;
            }
        }

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

        /// <inheritdoc cref="Consumer.GetSequenceStore"/>
        protected override ISequenceStore GetSequenceStore(IOffset offset) => GetSequenceStore((TOffset)offset);

        /// <summary>
        ///     Returns the <see cref="ISequenceStore" /> to be used to store the pending sequences.
        /// </summary>
        /// <param name="offset">
        ///     The offset may determine which store is being used. For example a dedicated sequence store is used per
        ///     each Kafka partition, since they may be processed concurrently.
        /// </param>
        /// <returns>
        ///     The <see cref="ISequenceStore" />.
        /// </returns>
        protected virtual ISequenceStore GetSequenceStore(TOffset offset) => base.GetSequenceStore(offset);
    }
}
