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
    /// <inheritdoc cref="IConsumerTransactionManager"/>
    public class ConsumerTransactionManager : IConsumerTransactionManager
    {
        private readonly ConsumerPipelineContext _context;

        private readonly List<ITransactional> _transactionalServices = new List<ITransactional>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsumerTransactionManager"/> class.
        /// </summary>
        /// <param name="context">
        ///     The current <see cref="ConsumerPipelineContext"/>.
        /// </param>
        public ConsumerTransactionManager(ConsumerPipelineContext context)
        {
            _context = context;
        }

        /// <summary>
        ///     Gets a value indicating whether the transaction has completed.
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        ///     Adds the specified service to the transaction participants to be called upon commit or rollback.
        /// </summary>
        /// <param name="transactionalService">
        ///     The service to be enlisted.
        /// </param>
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

        /// <inheritdoc cref="IConsumerTransactionManager.Commit" />
        public async Task Commit()
        {
            EnsureNotCompleted();
            IsCompleted = true;

            await _context.ServiceProvider.GetRequiredService<IPublisher>()
                .PublishAsync(new ConsumingCompletedEvent(_context))
                .ConfigureAwait(false);

            // TODO: At least once is ok?
            await _transactionalServices.ForEachAsync(service => service.Commit()).ConfigureAwait(false);
            await _context.Consumer.Commit(_context.Offsets).ConfigureAwait(false);
        }

        /// <inheritdoc cref="IConsumerTransactionManager.Rollback" />
        public async Task Rollback(Exception exception, bool commitOffsets = false)
        {
            EnsureNotCompleted();
            IsCompleted = true;

            try
            {
                await _context.ServiceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new ConsumingAbortedEvent(_context, exception))
                    .ConfigureAwait(false);

                await _transactionalServices.ForEachAsync(service => service.Rollback())
                    .ConfigureAwait(false);
            }
            finally
            {
                if (commitOffsets)
                    await _context.Consumer.Commit(_context.Offsets).ConfigureAwait(false);
                else
                    await _context.Consumer.Rollback(_context.Offsets).ConfigureAwait(false);
            }
        }

        private void EnsureNotCompleted()
        {
            if (IsCompleted)
                throw new InvalidOperationException("The transaction already completed.");
        }
    }
}
