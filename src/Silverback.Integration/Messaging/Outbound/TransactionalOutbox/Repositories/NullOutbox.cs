// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories.Model;

namespace Silverback.Messaging.Outbound.TransactionalOutbox.Repositories
{
    internal class NullOutbox : IOutboxReader, IOutboxWriter
    {
        public Task<int> GetLengthAsync() => Task.FromResult(0);

        public Task<TimeSpan> GetMaxAgeAsync() => Task.FromResult(TimeSpan.Zero);

        public Task<IReadOnlyCollection<OutboxStoredMessage>> ReadAsync(int count) =>
            Task.FromResult<IReadOnlyCollection<OutboxStoredMessage>>(Array.Empty<OutboxStoredMessage>());

        public Task AcknowledgeAsync(OutboxStoredMessage outboxMessage) => Task.CompletedTask;

        public Task AcknowledgeAsync(IEnumerable<OutboxStoredMessage> outboxMessages) =>
            throw new NotImplementedException();

        public Task RetryAsync(OutboxStoredMessage outboxMessage) => throw new NotSupportedException();

        public Task RetryAsync(IEnumerable<OutboxStoredMessage> outboxMessages) =>
            throw new NotImplementedException();

        public Task WriteAsync(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            string endpointName,
            string actualEndpointName) =>
            throw new NotSupportedException();

        public Task CommitAsync() => Task.CompletedTask;

        public Task RollbackAsync() => Task.CompletedTask;
    }
}
