// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Inbound.Transaction
{
    /// <inheritdoc cref="IConsumerTransactionManager" />
    public sealed class ConsumerTransactionManager : IConsumerTransactionManager
    {
        private readonly ConsumerPipelineContext _context;

        private readonly ISilverbackIntegrationLogger<ConsumerTransactionManager> _logger;

        private readonly List<ITransactional> _transactionalServices = new();

        private bool _isAborted;

        private bool _isCommitted;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConsumerTransactionManager" /> class.
        /// </summary>
        /// <param name="context">
        ///     The current <see cref="ConsumerPipelineContext" />.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackLogger" />.
        /// </param>
        public ConsumerTransactionManager(
            ConsumerPipelineContext context,
            ISilverbackIntegrationLogger<ConsumerTransactionManager> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc cref="IConsumerTransactionManager.IsCompleted" />
        public bool IsCompleted => _isAborted || _isCommitted;

        /// <inheritdoc cref="IConsumerTransactionManager.Enlist" />
        public void Enlist(ITransactional transactionalService)
        {
            Check.NotNull(transactionalService, nameof(transactionalService));

            if (IsCompleted)
                throw new InvalidOperationException("The transaction already completed.");

            // ReSharper disable once InconsistentlySynchronizedField
            if (_transactionalServices.Contains(transactionalService))
                return;

            lock (_transactionalServices)
            {
                if (_transactionalServices.Contains(transactionalService))
                    return;

                _transactionalServices.Add(transactionalService);
            }
        }

        /// <inheritdoc cref="IConsumerTransactionManager.CommitAsync" />
        public async Task CommitAsync()
        {
            if (_isCommitted)
            {
                _logger.LogTraceWithMessageInfo(
                    IntegrationEventIds.LowLevelTracing,
                    "Not committing consumer transaction because it was already committed.",
                    _context);

                return;
            }

            if (_isAborted)
                throw new InvalidOperationException("The transaction already aborted.");

            _isCommitted = true;

            _logger.LogTraceWithMessageInfo(
                IntegrationEventIds.LowLevelTracing,
                "Committing consumer transaction...",
                _context);

            // TODO: At least once is ok? (Consider that the DbContext might have been committed already.
            await _transactionalServices.ForEachAsync(service => service.CommitAsync()).ConfigureAwait(false);
            await _context.Consumer.CommitAsync(_context.GetBrokerMessageIdentifiers()).ConfigureAwait(false);

            _logger.LogTraceWithMessageInfo(
                IntegrationEventIds.LowLevelTracing,
                "Consumer transaction committed.",
                _context);
        }

        /// <inheritdoc cref="IConsumerTransactionManager.RollbackAsync" />
        public async Task<bool> RollbackAsync(
            Exception? exception,
            bool commitConsumer = false,
            bool throwIfAlreadyCommitted = true,
            bool stopConsuming = true)
        {
            if (_isAborted)
            {
                _logger.LogTraceWithMessageInfo(
                    IntegrationEventIds.LowLevelTracing,
                    "Not aborting consumer transaction because it was already aborted.",
                    _context);

                return false;
            }

            if (_isCommitted)
            {
                if (throwIfAlreadyCommitted)
                    throw new InvalidOperationException("The transaction already completed.");

                return false;
            }

            _isAborted = true;

            _logger.LogTraceWithMessageInfo(
                IntegrationEventIds.LowLevelTracing,
                exception,
                "Aborting consumer transaction...",
                _context);

            try
            {
                await _transactionalServices.ForEachAsync(service => service.RollbackAsync())
                    .ConfigureAwait(false);
            }
            finally
            {
                if (commitConsumer)
                {
                    await _context.Consumer.CommitAsync(_context.GetBrokerMessageIdentifiers()).ConfigureAwait(false);
                }
                else
                {
                    if (stopConsuming)
                        _context.Consumer.Stop();

                    await _context.Consumer.RollbackAsync(_context.GetBrokerMessageIdentifiers()).ConfigureAwait(false);
                }
            }

            _logger.LogTraceWithMessageInfo(
                IntegrationEventIds.LowLevelTracing,
                "Consumer transaction aborted.",
                _context);

            return true;
        }
    }
}
