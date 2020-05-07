// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Database;
using Silverback.Database.Model;
using Silverback.Infrastructure;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    /// <summary>
    ///     <para>
    ///         Used by the <see cref="LoggedInboundConnector" /> to keep track of each processed message and
    ///         guarantee that each one is processed only once.
    ///     </para>
    ///     <para>
    ///         An <see cref="IDbContext" /> is used to store the log into the database.
    ///     </para>
    /// </summary>
    // TODO: Test
    public sealed class DbInboundLog : RepositoryBase<InboundLogEntry>, IInboundLog, IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        /// <summary>
        ///     Initializes a new instance of the <see cref="DbInboundLog" /> class.
        /// </summary>
        /// <param name="dbContext">
        ///     The <see cref="IDbContext" /> to use as storage.
        /// </param>
        public DbInboundLog(IDbContext dbContext)
            : base(dbContext)
        {
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "SA1009", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public async Task Add(IRawInboundEnvelope envelope)
        {
            if (envelope == null)
                throw new ArgumentNullException(nameof(envelope));

            await _semaphore.WaitAsync();

            try
            {
                DbSet.Add(
                    new InboundLogEntry
                    {
                        MessageId = envelope.Headers.GetValue(DefaultMessageHeaders.MessageId, true)!,
                        ConsumerGroupName = envelope.Endpoint.GetUniqueConsumerGroupName(),
                        Consumed = DateTime.UtcNow
                    });
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc />
        public async Task Commit()
        {
            await _semaphore.WaitAsync();

            try
            {
                // Call SaveChanges, in case it isn't called by a subscriber
                await DbContext.SaveChangesAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc />
        public Task Rollback()
        {
            // Nothing to do, just not saving the changes made to the DbContext
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<bool> Exists(IRawInboundEnvelope envelope)
        {
            if (envelope == null)
                throw new ArgumentNullException(nameof(envelope));

            var key = envelope.Headers.GetValue(DefaultMessageHeaders.MessageId, true);
            var consumerGroupName = envelope.Endpoint.GetUniqueConsumerGroupName();
            return DbSet.AsQueryable().AnyAsync(m => m.MessageId == key && m.ConsumerGroupName == consumerGroupName);
        }

        /// <inheritdoc />
        public Task<int> GetLength() => DbSet.AsQueryable().CountAsync();

        /// <inheritdoc />
        public void Dispose()
        {
            _semaphore.Dispose();
        }
    }
}
