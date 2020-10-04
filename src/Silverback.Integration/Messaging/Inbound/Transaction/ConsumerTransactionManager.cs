// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Inbound.Transaction
{
    public class ConsumerTransactionManager : IConsumerTransactionManager
    {
        private readonly ConsumerPipelineContext _context;

        private readonly List<ITransactional> _transactionalServices = new List<ITransactional>();

        private bool _isCompleted;

        public ConsumerTransactionManager(ConsumerPipelineContext context)
        {
            _context = context;
        }

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

        public async Task Commit()
        {
            EnsureNotCompleted();
            _isCompleted = true;

            try
            {
                await _context.ServiceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new ConsumingCompletedEvent(_context))
                    .ConfigureAwait(false);

                // TODO: At least once is ok?
                await _transactionalServices.ForEachAsync(service => service.Commit()).ConfigureAwait(false);
                await _context.Consumer.Commit(_context.Offsets).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // TODO: Log
                throw;
            }
        }

        public async Task Rollback(Exception exception, bool commitOffsets = false)
        {
            EnsureNotCompleted();
            _isCompleted = true;

            try
            {
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
            catch (Exception ex)
            {
                // TODO: Log
                throw;
            }
        }

        private void EnsureNotCompleted()
        {
            if (_isCompleted)
                throw new InvalidOperationException("The transaction already completed.");
        }
    }
}
