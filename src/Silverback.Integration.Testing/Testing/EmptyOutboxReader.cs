// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Util;

namespace Silverback.Testing;

/// <summary>
///     An <see cref="IOutboxReader" /> that is always empty, used when the outbox is bypassed.
/// </summary>
internal sealed class EmptyOutboxReader : IOutboxReader
{
    public Task<IDisposableAsyncEnumerable<OutboxMessage>> GetAsync(int count) =>
        Task.FromResult<IDisposableAsyncEnumerable<OutboxMessage>>(new DisposableAsyncEnumerable<OutboxMessage>(Enumerable.Empty<OutboxMessage>().ToAsyncEnumerable()));

    public Task<int> GetLengthAsync() => Task.FromResult(0);

    public Task<TimeSpan> GetMaxAgeAsync() => Task.FromResult(TimeSpan.Zero);

    public Task AcknowledgeAsync(IEnumerable<OutboxMessage> outboxMessages) => Task.CompletedTask;
}
