// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
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

        _messageWrapper.WrapAndProduceAsync(
                Arg.Any<object>(),
                Arg.Any<SilverbackContext>(),
                Arg.Any<IProducer[]>())
            .Returns(call => call.Arg<IProducer[]>().All(producer => !producer.EndpointConfiguration.EnableSubscribing));
        _messageWrapper.WrapAndProduceBatchAsync(
                Arg.Any<IReadOnlyCollection<object>>(),
                Arg.Any<SilverbackContext>(),
                Arg.Any<IProducer[]>())
            .Returns(call => call.Arg<IProducer[]>().All(producer => !producer.EndpointConfiguration.EnableSubscribing));
        _messageWrapper.WrapAndProduceBatchAsync(
                Arg.Any<IEnumerable<object>>(),
                Arg.Any<SilverbackContext>(),
                Arg.Any<IProducer[]>())
            .Returns(call => call.Arg<IProducer[]>().All(producer => !producer.EndpointConfiguration.EnableSubscribing));
        _messageWrapper.WrapAndProduceBatchAsync(
                Arg.Any<IAsyncEnumerable<object>>(),
                Arg.Any<SilverbackContext>(),
                Arg.Any<IProducer[]>())
            .Returns(call => call.Arg<IProducer[]>().All(producer => !producer.EndpointConfiguration.EnableSubscribing));
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
        await _messageWrapper.Received(1).WrapAndProduceAsync(message1, Arg.Any<SilverbackContext>(), ArgIsProducers(producer1));
        await _messageWrapper.Received(1).WrapAndProduceAsync(message2, Arg.Any<SilverbackContext>(), ArgIsProducers(producer2));
    }

    [Fact]
    public async Task HandleAsync_ShouldProduceSingleMessageToMultipleProducers()
    {
        IProducer producer1 = AddProducer<TestEventOne>("topic1");
        IProducer producer2 = AddProducer<TestEventOne>("topic2");
        TestEventOne message = new();

        await _behavior.HandleAsync(message, _ => Next());

        _messageWrapper.ReceivedCalls().Count().Should().Be(1);
        await _messageWrapper.Received(1).WrapAndProduceAsync(message, Arg.Any<SilverbackContext>(), ArgIsProducers(producer1, producer2));
    }

    [Fact]
    public async Task HandleAsync_ShouldProduceCollection()
    {
        IProducer producer1 = AddProducer<TestEventOne>("topic1");
        IProducer producer2 = AddProducer<TestEventTwo>("topic2");
        List<TestEventOne> messages1 = [new TestEventOne(), new TestEventOne()];
        TestEventTwo[] messages2 = [new TestEventTwo(), new TestEventTwo()];

        await _behavior.HandleAsync(messages1, _ => Next());
        await _behavior.HandleAsync(messages2, _ => Next());

        _messageWrapper.ReceivedCalls().Count().Should().Be(2);
        await _messageWrapper.Received(1).WrapAndProduceBatchAsync(messages1, Arg.Any<SilverbackContext>(), ArgIsProducers(producer1));
        await _messageWrapper.Received(1).WrapAndProduceBatchAsync(messages2, Arg.Any<SilverbackContext>(), ArgIsProducers(producer2));
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
        await _messageWrapper.Received(1).WrapAndProduceBatchAsync(messages1, Arg.Any<SilverbackContext>(), ArgIsProducers(producer1));
        await _messageWrapper.Received(1).WrapAndProduceBatchAsync(messages2, Arg.Any<SilverbackContext>(), ArgIsProducers(producer2));
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
        await _messageWrapper.Received(1).WrapAndProduceBatchAsync(messages1, Arg.Any<SilverbackContext>(), ArgIsProducers(producer1));
        await _messageWrapper.Received(1).WrapAndProduceBatchAsync(messages2, Arg.Any<SilverbackContext>(), ArgIsProducers(producer2));
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
        await _messageWrapper.Received(1).WrapAndProduceAsync(message1, Arg.Any<SilverbackContext>(), ArgIsProducers(producer1));
        await _messageWrapper.Received(1).WrapAndProduceAsync(message2, Arg.Any<SilverbackContext>(), ArgIsProducers(producer2));
    }

    [Fact]
    public async Task HandleAsync_ShouldProduceTombstoneCollection()
    {
        IProducer producer1 = AddProducer<TestEventOne>("topic1");
        IProducer producer2 = AddProducer<TestEventTwo>("topic2");
        List<Tombstone<TestEventOne>> messages1 = [new Tombstone<TestEventOne>("1"), new Tombstone<TestEventOne>("2")];
        Tombstone<TestEventTwo>[] messages2 = [new Tombstone<TestEventTwo>("1"), new Tombstone<TestEventTwo>("2")];

        await _behavior.HandleAsync(messages1, _ => Next());
        await _behavior.HandleAsync(messages2, _ => Next());

        _messageWrapper.ReceivedCalls().Count().Should().Be(2);
        await _messageWrapper.Received(1).WrapAndProduceBatchAsync(messages1, Arg.Any<SilverbackContext>(), ArgIsProducers(producer1));
        await _messageWrapper.Received(1).WrapAndProduceBatchAsync(messages2, Arg.Any<SilverbackContext>(), ArgIsProducers(producer2));
    }

    [Fact]
    public async Task HandleAsync_ShouldProduceEnvelope()
    {
        IProducer producer = AddProducer<TestEventOne>("topic1");
        IProduceStrategyImplementation produceStrategyImplementation = Substitute.For<IProduceStrategyImplementation>();
        producer.EndpointConfiguration.Strategy.Build(Arg.Any<IServiceProvider>(), Arg.Any<ProducerEndpointConfiguration>())
            .Returns(produceStrategyImplementation);
        TestEventOne message = new();
        IOutboundEnvelope envelope = new OutboundEnvelope<TestEventOne>(
            message,
            null,
            producer.EndpointConfiguration.Endpoint.GetEndpoint(message, producer.EndpointConfiguration, Substitute.For<IServiceProvider>()),
            producer);

        await _behavior.HandleAsync(envelope, _ => Next());

        await produceStrategyImplementation.Received(1).ProduceAsync(Arg.Is<IOutboundEnvelope<TestEventOne>>(producedEnvelope => producedEnvelope.Message == message));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleAsync_ShouldContinuePipelineAccordingToEnableSubscribingFlag(bool enableSubscribing)
    {
        AddProducer<TestEventOne>("topic1", enableSubscribing);
        TestEventOne message = new();
        int nextBehaviorCalls = 0;

        await _behavior.HandleAsync(message, _ => Next(ref nextBehaviorCalls));

        nextBehaviorCalls.Should().Be(enableSubscribing ? 1 : 0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleAsync_ShouldContinuePipelineAccordingToEnableSubscribingFlag_WhenHandlingCollection(bool enableSubscribing)
    {
        AddProducer<TestEventOne>("topic1", enableSubscribing);
        List<TestEventOne> messages = [new TestEventOne(), new TestEventOne()];
        int nextBehaviorCalls = 0;

        await _behavior.HandleAsync(messages, _ => Next(ref nextBehaviorCalls));

        nextBehaviorCalls.Should().Be(enableSubscribing ? 1 : 0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleAsync_ShouldContinuePipelineAccordingToEnableSubscribingFlag_WhenHandlingEnumerable(bool enableSubscribing)
    {
        AddProducer<TestEventOne>("topic1", enableSubscribing);
        IEnumerable<TestEventOne> messages = new TestEventOne[] { new(), new() }.ToPureEnumerable();
        int nextBehaviorCalls = 0;

        await _behavior.HandleAsync(messages, _ => Next(ref nextBehaviorCalls));

        nextBehaviorCalls.Should().Be(enableSubscribing ? 1 : 0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleAsync_ShouldContinuePipelineAccordingToEnableSubscribingFlag_WhenHandlingAsyncEnumerable(bool enableSubscribing)
    {
        AddProducer<TestEventOne>("topic1", enableSubscribing);
        IAsyncEnumerable<TestEventOne> messages = new TestEventOne[] { new(), new() }.ToAsyncEnumerable();
        int nextBehaviorCalls = 0;

        await _behavior.HandleAsync(messages, _ => Next(ref nextBehaviorCalls));

        nextBehaviorCalls.Should().Be(enableSubscribing ? 1 : 0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleAsync_ShouldContinuePipelineAccordingToEnableSubscribingFlag_WhenHandlingEnvelope(bool enableSubscribing)
    {
        AddProducer<TestEventOne>("topic1", enableSubscribing);
        IProducer producer = AddProducer<TestEventOne>("topic1", enableSubscribing);
        IProduceStrategyImplementation produceStrategyImplementation = Substitute.For<IProduceStrategyImplementation>();
        producer.EndpointConfiguration.Strategy.Build(Arg.Any<IServiceProvider>(), Arg.Any<ProducerEndpointConfiguration>())
            .Returns(produceStrategyImplementation);
        TestEventOne message = new();
        IOutboundEnvelope envelope = new OutboundEnvelope<TestEventOne>(
            message,
            null,
            producer.EndpointConfiguration.Endpoint.GetEndpoint(message, producer.EndpointConfiguration, Substitute.For<IServiceProvider>()),
            producer);
        int nextBehaviorCalls = 0;

        await _behavior.HandleAsync(envelope, _ => Next(ref nextBehaviorCalls));

        nextBehaviorCalls.Should().Be(enableSubscribing ? 1 : 0);
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
