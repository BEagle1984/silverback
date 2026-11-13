// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Testing;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Testing.Testing;

[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
public class BypassOutboxWriterTests
{
    private readonly IProducer _producer = Substitute.For<IProducer>();

    private readonly BypassOutboxWriter _writer;

    public BypassOutboxWriterTests()
    {
        _producer.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic") { FriendlyName = "my-endpoint" });

        ProducerCollection producers = [_producer];

        _writer = new BypassOutboxWriter(producers);
    }

    [Fact]
    public async Task AddAsync_ShouldProduceSingleMessage()
    {
        OutboxMessage message = new([1, 2], [new MessageHeader("one", "1"), new MessageHeader("two", "2")], "my-endpoint");

        await _writer.AddAsync(message);

        await _producer.Received(1).RawProduceAsync(
            Arg.Is<byte[]?>(content => content!.SequenceEqual(new byte[] { 1, 2 })),
            Arg.Is<IReadOnlyCollection<MessageHeader>?>(headers => headers!.Count == 2 && headers.First().Name == "one"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_ShouldProduceEnumerableInOrder()
    {
        List<byte> produced = [];
        _producer
            .RawProduceAsync(Arg.Any<byte[]?>(), Arg.Any<IReadOnlyCollection<MessageHeader>?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                produced.Add(call.Arg<byte[]?>()![0]);
                return ValueTask.FromResult<IBrokerMessageIdentifier?>(null);
            });

        await _writer.AddAsync(
            [
                new OutboxMessage([1], null, "my-endpoint"),
                new OutboxMessage([2], null, "my-endpoint"),
                new OutboxMessage([3], null, "my-endpoint")
            ]);

        produced.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task AddAsync_ShouldProduceAsyncEnumerable()
    {
        static async IAsyncEnumerable<OutboxMessage> GetMessagesAsync()
        {
            yield return new OutboxMessage([1], null, "my-endpoint");
            await Task.Yield();
            yield return new OutboxMessage([2], null, "topic");
        }

        await _writer.AddAsync(GetMessagesAsync());

        await _producer.Received(2).RawProduceAsync(
            Arg.Any<byte[]?>(),
            Arg.Any<IReadOnlyCollection<MessageHeader>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_ShouldPropagateProduceErrors()
    {
        _producer
            .RawProduceAsync(Arg.Any<byte[]?>(), Arg.Any<IReadOnlyCollection<MessageHeader>?>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<IBrokerMessageIdentifier?>>(_ => throw new InvalidOperationException("produce failed"));

        await Should.ThrowAsync<InvalidOperationException>(_writer.AddAsync(new OutboxMessage([1], null, "my-endpoint")));
    }
}
