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
    /// <typeparam name="TBroker">
    ///     The type of the related <see cref="IBroker" /> implementation.
    /// </typeparam>
    /// <typeparam name="TEndpoint">
    ///     The type of the <see cref="IConsumerEndpoint" /> implementation used by this consumer implementation.
    /// </typeparam>
    /// <typeparam name="TIdentifier">
    ///     The type of the <see cref="IBrokerMessageIdentifier" /> used by this broker implementation.
    /// </typeparam>
    [SuppressMessage("", "CA1005", Justification = Justifications.NoWayToReduceTypeParameters)]
    public abstract class Consumer<TBroker, TEndpoint, TIdentifier> : Consumer
        where TBroker : IBroker
        where TEndpoint : IConsumerEndpoint
        where TIdentifier : IBrokerMessageIdentifier
    {
        private readonly ISilverbackIntegrationLogger<Consumer<TBroker, TEndpoint, TIdentifier>> _logger;

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
            ISilverbackIntegrationLogger<Consumer<TBroker, TEndpoint, TIdentifier>> logger)
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

        /// <inheritdoc cref="Consumer.CommitCoreAsync(IReadOnlyCollection{IBrokerMessageIdentifier})" />
        protected override Task CommitCoreAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers)
        {
            try
            {
                return CommitCoreAsync(brokerMessageIdentifiers.Cast<TIdentifier>().ToList());
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    IntegrationEventIds.ConsumerCommitError,
                    exception,
                    "Error occurred during commit. ({identifiers})",
                    string.Join(", ", brokerMessageIdentifiers.Select(identifier => identifier.Value)));
                throw;
            }
        }

        /// <inheritdoc cref="Consumer.RollbackCoreAsync(IReadOnlyCollection{IBrokerMessageIdentifier})" />
        protected override async Task RollbackCoreAsync(
            IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers)
        {
            try
            {
                await RollbackCoreAsync(brokerMessageIdentifiers.Cast<TIdentifier>().ToList()).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    IntegrationEventIds.ConsumerRollbackError,
                    exception,
                    "Error occurred during rollback. ({identifiers})",
                    string.Join(", ", brokerMessageIdentifiers.Select(identifier => identifier.Value)));
                throw;
            }
        }

        /// <inheritdoc cref="Consumer.CommitCoreAsync(IReadOnlyCollection{IBrokerMessageIdentifier})" />
        protected abstract Task CommitCoreAsync(IReadOnlyCollection<TIdentifier> brokerMessageIdentifiers);

        /// <inheritdoc cref="Consumer.RollbackCoreAsync(IReadOnlyCollection{IBrokerMessageIdentifier})" />
        protected abstract Task RollbackCoreAsync(IReadOnlyCollection<TIdentifier> brokerMessageIdentifiers);

        /// <inheritdoc cref="Consumer.GetSequenceStore" />
        protected override ISequenceStore GetSequenceStore(IBrokerMessageIdentifier brokerMessageIdentifier) =>
            GetSequenceStore((TIdentifier)brokerMessageIdentifier);

        /// <inheritdoc cref="Consumer.GetSequenceStore" />
        protected virtual ISequenceStore GetSequenceStore(TIdentifier brokerMessageIdentifier) =>
            base.GetSequenceStore(brokerMessageIdentifier);
    }
}
