// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Database;
using Silverback.Database.Model;
using Silverback.Infrastructure;
using Silverback.Messaging.Inbound.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Util;

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
    public class DbInboundLog : RepositoryBase<InboundLogEntry>, IInboundLog
    {
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

        /// <inheritdoc cref="IInboundLog.Add" />
        [SuppressMessage("", "SA1009", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public Task Add(IRawInboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            string messageId = envelope.Headers.GetValue(DefaultMessageHeaders.MessageId, true)!;
            string consumerGroupName = envelope.Endpoint.GetUniqueConsumerGroupName();

            var logEntry = new InboundLogEntry
            {
                MessageId = messageId,
                EndpointName = envelope.ActualEndpointName,
                ConsumerGroupName = consumerGroupName
            };

            DbSet.Add(logEntry);

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="ITransactional.Commit" />
        public async Task Commit()
        {
            // Call SaveChanges, in case it isn't called by a subscriber
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <inheritdoc cref="ITransactional.Rollback" />
        public Task Rollback()
        {
            // Nothing to do, just not saving the changes made to the DbContext
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="IInboundLog.Exists" />
        [SuppressMessage("", "SA1009", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public Task<bool> Exists(IRawInboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            string messageId = envelope.Headers.GetValue(DefaultMessageHeaders.MessageId, true)!;
            string consumerGroupName = envelope.Endpoint.GetUniqueConsumerGroupName();

            return DbSet.AsQueryable().AnyAsync(
                logEntry =>
                    logEntry.MessageId == messageId &&
                    logEntry.EndpointName == envelope.ActualEndpointName &&
                    logEntry.ConsumerGroupName == consumerGroupName);
        }

        /// <inheritdoc cref="IInboundLog.GetLength" />
        public Task<int> GetLength() => DbSet.AsQueryable().CountAsync();
    }
}
