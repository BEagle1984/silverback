// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Testing;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Testing.Testing;

public class EmptyOutboxReaderTests
{
    private readonly EmptyOutboxReader _reader = new();

    [Fact]
    public async Task GetAsync_ShouldReturnEmptyEnumerable()
    {
        using IDisposableAsyncEnumerable<OutboxMessage> messages = await _reader.GetAsync(10);

        List<OutboxMessage> result = [];
        await foreach (OutboxMessage message in messages)
        {
            result.Add(message);
        }

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetLengthAsync_ShouldReturnZero() => (await _reader.GetLengthAsync()).ShouldBe(0);

    [Fact]
    public async Task GetMaxAgeAsync_ShouldReturnZero() => (await _reader.GetMaxAgeAsync()).ShouldBe(TimeSpan.Zero);

    [Fact]
    public Task AcknowledgeAsync_ShouldNotThrow() => Should.NotThrowAsync(_reader.AcknowledgeAsync([new OutboxMessage([1], null, "endpoint")]));
}
