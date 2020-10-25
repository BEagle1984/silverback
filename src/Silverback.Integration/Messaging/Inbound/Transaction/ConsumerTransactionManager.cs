// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Inbound.Transaction
{
    /// <inheritdoc cref="IConsumerTransactionManager" />
    public sealed class ConsumerTransactionManager : IConsumerTransactionManager
    {
        private readonly ConsumerPipelineContext _context;

        private readonly List<ITransactional> _transactionalServices = new List<ITransactional>();

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConsumerTransactionManager" /> class.
        /// </summary>
        /// <param name="context">
        ///     The current <see cref="ConsumerPipelineContext" />.
        /// </param>
        public ConsumerTransactionManager(ConsumerPipelineContext context)
        {
            _context = context;
        }

        /// <inheritdoc cref="IConsumerTransactionManager.IsCompleted"/>
        public bool IsCompleted { get; private set; }

        /// <inheritdoc cref="IConsumerTransactionManager.Enlist"/>
        public void Enlist(ITransactional transactionalService)
        {
            Check.NotNull(transactionalService, nameof(transactionalService));
            EnsureNotCompleted();

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

        /// <inheritdoc cref="IConsumerTransactionManager.CommitAsync"/>
        public async Task CommitAsync()
        {
            EnsureNotCompleted();
            IsCompleted = true;

            // TODO: At least once is ok? (Consider that the DbContext might have been committed already.
            await _transactionalServices.ForEachAsync(service => service.CommitAsync()).ConfigureAwait(false);
            await _context.Consumer.CommitAsync(_context.Offsets).ConfigureAwait(false);
        }

        /// <inheritdoc cref="IConsumerTransactionManager.RollbackAsync" />
        public async Task RollbackAsync(Exception? exception, bool commitOffsets = false)
        {
            EnsureNotCompleted();
            IsCompleted = true;

            try
            {
                await _transactionalServices.ForEachAsync(service => service.RollbackAsync())
                    .ConfigureAwait(false);
            }
            finally
            {
                if (commitOffsets)
                    await _context.Consumer.CommitAsync(_context.Offsets).ConfigureAwait(false);
                else
                    await _context.Consumer.RollbackAsync(_context.Offsets).ConfigureAwait(false);
            }
        }

        private void EnsureNotCompleted()
        {
            if (IsCompleted)
                throw new InvalidOperationException("The transaction already completed.");
        }
    }
}
