// TODO: DELETE
// // Copyright (c) 2020 Sergio Aquilini
// // This code is licensed under MIT license (see LICENSE file for details)
//
// using System.Threading.Tasks;
// using Silverback.Database;
// using Silverback.Database.Model;
// using Silverback.Infrastructure;
// using Silverback.Messaging.Inbound.Transaction;
// using Silverback.Messaging.Messages;
// using Silverback.Util;
//
// namespace Silverback.Messaging.Inbound.ExactlyOnce.Repositories;
//
// /// <summary>
// ///     <para>
// ///         Used by the <see cref="LogExactlyOnceStrategy" /> to keep track of each processed message and
// ///         guarantee that each one is processed only once.
// ///     </para>
// ///     <para>
// ///         An <see cref="IDbContext" /> is used to store the log into the database.
// ///     </para>
// /// </summary>
// public class DbInboundLog : RepositoryBase<InboundLogEntry>, IInboundLog
// {
//     /// <summary>
//     ///     Initializes a new instance of the <see cref="DbInboundLog" /> class.
//     /// </summary>
//     /// <param name="dbContext">
//     ///     The <see cref="IDbContext" /> to use as storage.
//     /// </param>
//     public DbInboundLog(IDbContext dbContext)
//         : base(dbContext)
//     {
//     }
//
//     /// <inheritdoc cref="IInboundLog.AddAsync" />
//     public Task AddAsync(IRawInboundEnvelope envelope)
//     {
//         Check.NotNull(envelope, nameof(envelope));
//
//         string messageId = envelope.Headers.GetValue(DefaultMessageHeaders.MessageId, true)!;
//         string consumerGroupName = envelope.Endpoint.Configuration.GetUniqueConsumerGroupName();
//
//         InboundLogEntry logEntry = new()
//         {
//             MessageId = messageId,
//             EndpointName = envelope.Endpoint.RawName,
//             ConsumerGroupName = consumerGroupName
//         };
//
//         DbSet.Add(logEntry);
//
//         return Task.CompletedTask;
//     }
//
//     /// <inheritdoc cref="ITransactional.CommitAsync" />
//     public async Task CommitAsync()
//     {
//         // Call SaveChanges, in case it isn't called by a subscriber
//         await DbContext.SaveChangesAsync().ConfigureAwait(false);
//     }
//
//     /// <inheritdoc cref="ITransactional.RollbackAsync" />
//     public Task RollbackAsync()
//     {
//         // Nothing to do, just not saving the changes made to the DbContext
//         return Task.CompletedTask;
//     }
//
//     /// <inheritdoc cref="IInboundLog.ExistsAsync" />
//     public Task<bool> ExistsAsync(IRawInboundEnvelope envelope)
//     {
//         Check.NotNull(envelope, nameof(envelope));
//
//         string messageId = envelope.Headers.GetValue(DefaultMessageHeaders.MessageId, true)!;
//         string consumerGroupName = envelope.Endpoint.Configuration.GetUniqueConsumerGroupName();
//
//         return DbSet.AsQueryable().AnyAsync(
//             logEntry =>
//                 logEntry.MessageId == messageId &&
//                 logEntry.EndpointName == envelope.Endpoint.RawName &&
//                 logEntry.ConsumerGroupName == consumerGroupName);
//     }
//
//     /// <inheritdoc cref="IInboundLog.GetLengthAsync" />
//     public Task<int> GetLengthAsync() => DbSet.AsQueryable().CountAsync();
// }
