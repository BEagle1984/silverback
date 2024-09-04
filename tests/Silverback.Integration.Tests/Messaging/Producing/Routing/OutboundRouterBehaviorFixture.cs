// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing;
using Silverback.Messaging.Producing.Routing;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.Routing;

public class OutboundRouterBehaviorFixture
{
    private readonly ProducerCollection _producers = [];

    private readonly IMessageWrapper _messageWrapper;

    private readonly OutboundRouterBehavior _behavior;

    public OutboundRouterBehaviorFixture()
    {
        _messageWrapper = Substitute.For<IMessageWrapper>();
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Context.Returns(new SilverbackContext(Substitute.For<IServiceProvider>()));
        _behavior = new OutboundRouterBehavior(publisher, _messageWrapper, _producers);
    }

    [Fact]
    public async Task HandleAsync_ShouldContinuePipeline_WhenNoMatchingProducer()
    {
        AddProducer<TestEventTwo>("topic2");
        int nextBehaviorCalls = 0;

        await _behavior.HandleAsync(new TestEventOne(), _ => Next(ref nextBehaviorCalls));

        nextBehaviorCalls.Should().Be(1);
        _messageWrapper.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ShouldProduceSingleMessage()
    {
        IProducer producer1 = AddProducer<TestEventOne>("topic1");
        IProducer producer2 = AddProducer<TestEventTwo>("topic2");
        TestEventOne message1 = new();
        TestEventTwo message2 = new();

        await _behavior.HandleAsync(message1, _ => Next());
        await _behavior.HandleAsync(message2, _ => Next());

        _messageWrapper.ReceivedCalls().Count().Should().Be(2);
        await _messageWrapper.Received(1).WrapAndProduceAsync(message1, Arg.Any<IPublisher>(), ArgIsProducers(producer1));
        await _messageWrapper.Received(1).WrapAndProduceAsync(message2, Arg.Any<IPublisher>(), ArgIsProducers(producer2));
    }

    [Fact]
    public async Task HandleAsync_ShouldProduceSingleMessageToMultipleProducers()
    {
        IProducer producer1 = AddProducer<TestEventOne>("topic1");
        IProducer producer2 = AddProducer<TestEventOne>("topic2");
        TestEventOne message = new();

        await _behavior.HandleAsync(message, _ => Next());

        _messageWrapper.ReceivedCalls().Count().Should().Be(1);
        await _messageWrapper.Received(1).WrapAndProduceAsync(message, Arg.Any<IPublisher>(), ArgIsProducers(producer1, producer2));
    }

    [Fact]
    public async Task HandleAsync_ShouldProduceCollection()
    {
        IProducer producer1 = AddProducer<TestEventOne>("topic1");
        IProducer producer2 = AddProducer<TestEventTwo>("topic2");
        List<TestEventOne> messages1 = [new(), new()];
        TestEventTwo[] messages2 = [new(), new()];

        await _behavior.HandleAsync(messages1, _ => Next());
        await _behavior.HandleAsync(messages2, _ => Next());

        _messageWrapper.ReceivedCalls().Count().Should().Be(2);
        await _messageWrapper.Received(1).WrapAndProduceBatchAsync(messages1, Arg.Any<IPublisher>(), ArgIsProducers(producer1));
        await _messageWrapper.Received(1).WrapAndProduceBatchAsync(messages2, Arg.Any<IPublisher>(), ArgIsProducers(producer2));
    }

    [Fact]
    public async Task HandleAsync_ShouldProduceEnumerable()
    {
        IProducer producer1 = AddProducer<TestEventOne>("topic1");
        IProducer producer2 = AddProducer<TestEventTwo>("topic2");
        IEnumerable<TestEventOne> messages1 = new TestEventOne[] { new(), new() }.ToPureEnumerable();
        IEnumerable<TestEventTwo> messages2 = new TestEventTwo[] { new(), new() }.ToPureEnumerable();

        await _behavior.HandleAsync(messages1, _ => Next());
        await _behavior.HandleAsync(messages2, _ => Next());

        _messageWrapper.ReceivedCalls().Count().Should().Be(2);
        await _messageWrapper.Received(1).WrapAndProduceBatchAsync(messages1, Arg.Any<IPublisher>(), ArgIsProducers(producer1));
        await _messageWrapper.Received(1).WrapAndProduceBatchAsync(messages2, Arg.Any<IPublisher>(), ArgIsProducers(producer2));
    }

    [Fact]
    public async Task HandleAsync_ShouldProduceAsyncEnumerable()
    {
        IProducer producer1 = AddProducer<TestEventOne>("topic1");
        IProducer producer2 = AddProducer<TestEventTwo>("topic2");
        IAsyncEnumerable<TestEventOne> messages1 = new TestEventOne[] { new(), new() }.ToAsyncEnumerable();
        IAsyncEnumerable<TestEventTwo> messages2 = new TestEventTwo[] { new(), new() }.ToAsyncEnumerable();

        await _behavior.HandleAsync(messages1, _ => Next());
        await _behavior.HandleAsync(messages2, _ => Next());

        _messageWrapper.ReceivedCalls().Count().Should().Be(2);
        await _messageWrapper.Received(1).WrapAndProduceBatchAsync(messages1, Arg.Any<IPublisher>(), ArgIsProducers(producer1));
        await _messageWrapper.Received(1).WrapAndProduceBatchAsync(messages2, Arg.Any<IPublisher>(), ArgIsProducers(producer2));
    }

    [Fact]
    public async Task HandleAsync_ShouldProduceSingleTombstone()
    {
        IProducer producer1 = AddProducer<TestEventOne>("topic1");
        IProducer producer2 = AddProducer<TestEventTwo>("topic2");
        Tombstone<TestEventOne> message1 = new("1");
        Tombstone<TestEventTwo> message2 = new("2");

        await _behavior.HandleAsync(message1, _ => Next());
        await _behavior.HandleAsync(message2, _ => Next());

        _messageWrapper.ReceivedCalls().Count().Should().Be(2);
        await _messageWrapper.Received(1).WrapAndProduceAsync(message1, Arg.Any<IPublisher>(), ArgIsProducers(producer1));
        await _messageWrapper.Received(1).WrapAndProduceAsync(message2, Arg.Any<IPublisher>(), ArgIsProducers(producer2));
    }

    [Fact]
    public async Task HandleAsync_ShouldProduceTombstoneCollection()
    {
        IProducer producer1 = AddProducer<TestEventOne>("topic1");
        IProducer producer2 = AddProducer<TestEventTwo>("topic2");
        List<Tombstone<TestEventOne>> messages1 = [new("1"), new("2")];
        Tombstone<TestEventTwo>[] messages2 = [new("1"), new("2")];

        await _behavior.HandleAsync(messages1, _ => Next());
        await _behavior.HandleAsync(messages2, _ => Next());

        _messageWrapper.ReceivedCalls().Count().Should().Be(2);
        await _messageWrapper.Received(1).WrapAndProduceBatchAsync(messages1, Arg.Any<IPublisher>(), ArgIsProducers(producer1));
        await _messageWrapper.Received(1).WrapAndProduceBatchAsync(messages2, Arg.Any<IPublisher>(), ArgIsProducers(producer2));
    }

    private static ValueTask<IReadOnlyCollection<object?>> Next() => ValueTask.FromResult<IReadOnlyCollection<object?>>([]);

    private static ValueTask<IReadOnlyCollection<object?>> Next(ref int nextBehaviorCalls)
    {
        nextBehaviorCalls++;
        return ValueTask.FromResult<IReadOnlyCollection<object?>>([]);
    }

    private static IProducer[] ArgIsProducers(params IProducer[] expectedProducers) =>
        Arg.Is<IProducer[]>(producers => producers.SequenceEqual(expectedProducers));

    private IProducer AddProducer<TMessage>(string topic, bool enableSubscribing = false)
    {
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration(topic, typeof(TMessage))
            {
                Strategy = Substitute.For<IProduceStrategy>(),
                EnableSubscribing = enableSubscribing
            });
        _producers.Add(producer);
        return producer;
    }
}
