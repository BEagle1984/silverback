// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Inbound.Transaction
{
    public class ConsumerTransactionManager : IConsumerTransactionManager
    {
        private readonly IConsumer _consumer;

        private readonly ConcurrentBag<IOffset> _offsets = new ConcurrentBag<IOffset>();

        private readonly List<ITransactional> _transactionalServices = new List<ITransactional>();

        private bool _isCompleted;

        public ConsumerTransactionManager(IConsumer consumer)
        {
            _consumer = consumer;
        }

        public void AddOffset(IOffset offset)
        {
            Check.NotNull(offset, nameof(offset));
            EnsureNotCompleted();

            _offsets.Add(offset);
        }

        public void AddOffsets(IEnumerable<IOffset> offsets)
        {
            Check.NotNull(offsets, nameof(offsets));
            EnsureNotCompleted();

            offsets.ForEach(offset => _offsets.Add(offset));
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
                // TODO: At least once is ok?
                await _transactionalServices.ForEachAsync(service => service.Commit())
                    .ConfigureAwait(false);
                await _consumer.Commit(_offsets.ToArray()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // TODO: Log
                throw;
            }
        }

        public async Task Rollback(bool commitOffsets = false)
        {
            EnsureNotCompleted();
            _isCompleted = true;

            try
            {
                try
                {
                    await _transactionalServices.ForEachAsync(service => service.Rollback())
                        .ConfigureAwait(false);
                }
                finally
                {
                    if (commitOffsets)
                        await _consumer.Commit(_offsets.ToArray()).ConfigureAwait(false);
                    else
                        await _consumer.Rollback(_offsets.ToArray()).ConfigureAwait(false);
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
