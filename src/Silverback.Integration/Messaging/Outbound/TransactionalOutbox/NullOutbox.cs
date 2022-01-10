// TODO: REMOVE?

// // Copyright (c) 2020 Sergio Aquilini
// // This code is licensed under MIT license (see LICENSE file for details)
//
// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
//
// namespace Silverback.Messaging.Outbound.TransactionalOutbox;
//
// internal sealed class NullOutbox : IOutboxReader, IOutboxWriter
// {
//     public Task<int> GetLengthAsync() => Task.FromResult(0);
//
//     public Task<TimeSpan> GetMaxAgeAsync() => Task.FromResult(TimeSpan.Zero);
//
//     public Task<IReadOnlyCollection<OutboxMessage>> GetAsync(int count) =>
//         Task.FromResult<IReadOnlyCollection<OutboxMessage>>(Array.Empty<OutboxMessage>());
//
//     public Task AcknowledgeAsync(IEnumerable<OutboxMessage> outboxMessages) => throw new NotSupportedException();
//
//     public Task AddAsync(OutboxMessage outboxMessage) => throw new NotSupportedException();
//
//     public Task CommitAsync() => Task.CompletedTask;
//
//     public Task RollbackAsync() => Task.CompletedTask;
// }
