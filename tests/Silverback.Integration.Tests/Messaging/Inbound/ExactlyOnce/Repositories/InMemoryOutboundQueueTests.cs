// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories.Model;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Inbound.ExactlyOnce.Repositories;

public class InMemoryOutboundQueueTests
{
    private readonly InMemoryOutbox _queue;

    private readonly IOutboundEnvelope _sampleOutboundEnvelope = new OutboundEnvelope(
        new TestEventOne { Content = "Test" },
        null,
        TestProducerEndpoint.GetDefault());

    public InMemoryOutboundQueueTests()
    {
        _queue = new InMemoryOutbox(new TransactionalListSharedItems<OutboxStoredMessage>());
    }

    [Fact]
    public async Task Enqueue_MultipleTimesInParallelNoCommit_QueueLooksEmpty()
    {
        await Task.WhenAll(
            Enumerable.Range(0, 3)
                .Select(
                    _ => _queue.WriteAsync(
                        _sampleOutboundEnvelope.Message,
                        _sampleOutboundEnvelope.RawMessage.ReadAll(),
                        _sampleOutboundEnvelope.Headers,
                        _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
                        _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
                        new byte[10])));

        (await _queue.GetLengthAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Enqueue_MultipleTimesInParallelAndCommit_QueueFilled()
    {
        await Task.WhenAll(
            Enumerable.Range(0, 3)
                .Select(
                    _ => _queue.WriteAsync(
                        _sampleOutboundEnvelope.Message,
                        _sampleOutboundEnvelope.RawMessage.ReadAll(),
                        _sampleOutboundEnvelope.Headers,
                        _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
                        _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
                        new byte[10])));

        await _queue.CommitAsync();

        (await _queue.GetLengthAsync()).Should().Be(3);
    }

    [Fact]
    public async Task Enqueue_MultipleTimesInParallelAndRollback_QueueIsEmpty()
    {
        await Task.WhenAll(
            Enumerable.Range(0, 3)
                .Select(
                    _ => _queue.WriteAsync(
                        _sampleOutboundEnvelope.Message,
                        _sampleOutboundEnvelope.RawMessage.ReadAll(),
                        _sampleOutboundEnvelope.Headers,
                        _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
                        _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
                        new byte[10])));

        await _queue.RollbackAsync();

        (await _queue.GetLengthAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Enqueue_SomeEnvelopes_OnlyCommittedEnvelopesAreEnqueued()
    {
        await _queue.WriteAsync(
            _sampleOutboundEnvelope.Message,
            _sampleOutboundEnvelope.RawMessage.ReadAll(),
            _sampleOutboundEnvelope.Headers,
            _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
            _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
            new byte[10]);
        await _queue.CommitAsync();
        await _queue.WriteAsync(
            _sampleOutboundEnvelope.Message,
            _sampleOutboundEnvelope.RawMessage.ReadAll(),
            _sampleOutboundEnvelope.Headers,
            _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
            _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
            new byte[10]);
        await _queue.RollbackAsync();
        await _queue.WriteAsync(
            _sampleOutboundEnvelope.Message,
            _sampleOutboundEnvelope.RawMessage.ReadAll(),
            _sampleOutboundEnvelope.Headers,
            _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
            _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
            new byte[10]);
        await _queue.CommitAsync();

        (await _queue.GetLengthAsync()).Should().Be(2);
    }

    [Theory]
    [InlineData(3, 3)]
    [InlineData(5, 5)]
    [InlineData(10, 5)]
    public async Task Dequeue_WithCommittedEnvelopes_ExpectedEnvelopesReturned(
        int count,
        int expected)
    {
        for (int i = 0; i < 5; i++)
        {
            await _queue.WriteAsync(
                _sampleOutboundEnvelope.Message,
                _sampleOutboundEnvelope.RawMessage.ReadAll(),
                _sampleOutboundEnvelope.Headers,
                _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
                _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
                new byte[10]);
        }

        await _queue.CommitAsync();

        IReadOnlyCollection<OutboxStoredMessage> result = await _queue.ReadAsync(count);

        result.Should().HaveCount(expected);
    }

    [Fact]
    public async Task AcknowledgeAndRetry_RetriedAreStillEnqueued()
    {
        for (int i = 0; i < 5; i++)
        {
            await _queue.WriteAsync(
                _sampleOutboundEnvelope.Message,
                _sampleOutboundEnvelope.RawMessage.ReadAll(),
                _sampleOutboundEnvelope.Headers,
                _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
                _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
                new byte[10]);
        }

        await _queue.CommitAsync();

        OutboxStoredMessage[] result = (await _queue.ReadAsync(5)).ToArray();

        await _queue.AcknowledgeAsync(result[0]);
        await _queue.RetryAsync(result[1]);
        await _queue.AcknowledgeAsync(result[2]);
        await _queue.RetryAsync(result[3]);
        await _queue.AcknowledgeAsync(result[4]);

        (await _queue.GetLengthAsync()).Should().Be(2);
    }
}
