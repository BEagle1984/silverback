// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling;

public class MoveMessageErrorPolicyFixture
{
    private readonly IServiceProvider _serviceProvider;

    public MoveMessageErrorPolicyFixture()
    {
        ServiceCollection services = [];

        services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void CanHandle_ShouldReturnTrue_WhenMessageIsNotInSequence()
    {
        IErrorPolicyImplementation policy = new MoveMessageErrorPolicy("topic2").Build(_serviceProvider);
        InboundEnvelope envelope = new(
            "hey oh!",
            Stream.Null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        bool result = policy.CanHandle(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            new InvalidOperationException("test"));

        result.Should().BeTrue();
    }

    [Fact]
    public void CanHandle_ShouldReturnFalse_WhenMessageIsInSequence()
    {
        IErrorPolicyImplementation policy = new MoveMessageErrorPolicy("topic2").Build(_serviceProvider);
        TestConsumerEndpointConfiguration endpointConfiguration = new("test")
        {
            Batch = new BatchSettings
            {
                Size = 10
            }
        };
        InboundEnvelope envelope = new(
            "hey oh!",
            Stream.Null,
            null,
            new TestConsumerEndpoint("test", endpointConfiguration),
            Substitute.For<IConsumer>(),
            new TestOffset());

        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider);
        context.SetSequence(new BatchSequence("batch", context), false);

        bool result = policy.CanHandle(
            context,
            new InvalidOperationException("test"));

        result.Should().BeFalse();
    }

    [Fact]
    public void CanHandle_ShouldReturnFalse_WhenMessageIsInRawSequence()
    {
        IErrorPolicyImplementation policy = new MoveMessageErrorPolicy("topic2").Build(_serviceProvider);
        InboundEnvelope envelope = new(
            "hey oh!",
            Stream.Null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider);
        context.SetSequence(new ChunkSequence("123", 5, context), false);

        bool result = policy.CanHandle(
            context,
            new InvalidOperationException("test"));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldProduceMessage()
    {
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic2"));
        _serviceProvider.GetRequiredService<IProducerCollection>().As<ProducerCollection>().Add(producer);

        IErrorPolicyImplementation policy = new MoveMessageErrorPolicy("topic2").Build(_serviceProvider);
        InboundEnvelope envelope = new(
            "hey oh!",
            Stream.Null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            new InvalidOperationException("test"));

        await producer.Received(1).ProduceAsync(Arg.Any<IOutboundEnvelope>());
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldPreserveMessageContent()
    {
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic2"));
        _serviceProvider.GetRequiredService<IProducerCollection>().As<ProducerCollection>().Add(producer);

        IErrorPolicyImplementation policy = new MoveMessageErrorPolicy("topic2").Build(_serviceProvider);

        TestEventOne message = new() { Content = "Test" };
        InboundEnvelope inboundEnvelope = new(
            message,
            new MemoryStream(BytesUtil.GetRandomBytes()),
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(inboundEnvelope, _serviceProvider),
            new InvalidOperationException("test"));

        await producer.Received(1).ProduceAsync(Arg.Is<IOutboundEnvelope>(outboundEnvelope => outboundEnvelope.Message == message));
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldPreserveRawMessageContent_WhenMessageWasNotDeserialized()
    {
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic2"));
        _serviceProvider.GetRequiredService<IProducerCollection>().As<ProducerCollection>().Add(producer);

        IErrorPolicyImplementation policy = new MoveMessageErrorPolicy("topic2").Build(_serviceProvider);

        byte[] rawContent = BytesUtil.GetRandomBytes();
        RawInboundEnvelope inboundEnvelope = new(
            new MemoryStream(rawContent),
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(inboundEnvelope, _serviceProvider),
            new InvalidOperationException("test"));

        await producer.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope>(
                outboundEnvelope =>
                    outboundEnvelope.RawMessage.ReReadAll()!.SequenceEqual(rawContent)));
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldPreserveHeaders()
    {
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic2"));
        _serviceProvider.GetRequiredService<IProducerCollection>().As<ProducerCollection>().Add(producer);

        IErrorPolicyImplementation policy = new MoveMessageErrorPolicy("topic2").Build(_serviceProvider);

        MessageHeaderCollection headers = new()
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };
        InboundEnvelope inboundEnvelope = new(
            new TestEventOne(),
            new MemoryStream(BytesUtil.GetRandomBytes()),
            headers,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(inboundEnvelope, _serviceProvider),
            new InvalidOperationException("test"));

        await producer.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope>(
                outboundEnvelope =>
                    outboundEnvelope.Headers.GetValue("key1", true) == "value1" &&
                    outboundEnvelope.Headers.GetValue("key2", true) == "value2"));
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldTransformHeaders()
    {
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic2"));
        _serviceProvider.GetRequiredService<IProducerCollection>().As<ProducerCollection>().Add(producer);

        MoveMessageErrorPolicy policy = new("topic2")
        {
            TransformMessageAction = (outboundEnvelope, ex) =>
            {
                outboundEnvelope.Headers.Add("error", ex.GetType().Name);
            }
        };
        IErrorPolicyImplementation policyImplementation = policy.Build(_serviceProvider);

        InboundEnvelope inboundEnvelope = new(
            new TestEventOne(),
            new MemoryStream(BytesUtil.GetRandomBytes()),
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        await policyImplementation.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(inboundEnvelope, _serviceProvider),
            new InvalidOperationException("test"));

        await producer.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope>(
                outboundEnvelope =>
                    outboundEnvelope.Headers.GetValue("error", true) == "InvalidOperationException"));
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldReturnTrue()
    {
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic2"));
        _serviceProvider.GetRequiredService<IProducerCollection>().As<ProducerCollection>().Add(producer);

        IErrorPolicyImplementation policy = new MoveMessageErrorPolicy("topic2").Build(_serviceProvider);
        InboundEnvelope envelope = new(
            "hey oh!",
            Stream.Null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        bool result = await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            new InvalidOperationException("test"));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldCommitOffsetButAbortTransaction()
    {
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic2"));
        _serviceProvider.GetRequiredService<IProducerCollection>().As<ProducerCollection>().Add(producer);

        IErrorPolicyImplementation policy = new MoveMessageErrorPolicy("topic2").Build(_serviceProvider);
        InboundEnvelope envelope = new(
            "hey oh!",
            Stream.Null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        IConsumerTransactionManager? transactionManager = Substitute.For<IConsumerTransactionManager>();
        await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider, transactionManager),
            new InvalidOperationException("test"));

        await transactionManager.Received(1).RollbackAsync(Arg.Any<InvalidOperationException>(), true);
    }
}
