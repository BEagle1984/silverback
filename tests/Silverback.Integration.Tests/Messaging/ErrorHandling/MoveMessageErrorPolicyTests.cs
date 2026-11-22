// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling;

public class MoveMessageErrorPolicyTests
{
    private readonly TestErrorPolicyCallback _callback = new();

    private readonly IServiceProvider _serviceProvider;

    public MoveMessageErrorPolicyTests()
    {
        ServiceCollection services = [];

        services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker()
            .AddSingletonErrorPolicyCallback(_callback);

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

        result.ShouldBeTrue();
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

        result.ShouldBeFalse();
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

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldProduceMessage()
    {
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic2"));
        ((ProducerCollection)_serviceProvider.GetRequiredService<IProducerCollection>()).Add(producer);

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
        ((ProducerCollection)_serviceProvider.GetRequiredService<IProducerCollection>()).Add(producer);

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
        ((ProducerCollection)_serviceProvider.GetRequiredService<IProducerCollection>()).Add(producer);

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
            Arg.Is<IOutboundEnvelope>(outboundEnvelope =>
                outboundEnvelope.RawMessage.ReReadAll()!.SequenceEqual(rawContent)));
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldRewindRawMessageAndBinaryMessageContent()
    {
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic2"));
        ((ProducerCollection)_serviceProvider.GetRequiredService<IProducerCollection>()).Add(producer);

        IErrorPolicyImplementation policy = new MoveMessageErrorPolicy("topic2").Build(_serviceProvider);

        // Both the raw message and the content of the binary message were read by the deserializer or by the subscriber
        MemoryStream rawMessage = new(BytesUtil.GetRandomBytes());
        rawMessage.Seek(0, SeekOrigin.End);
        MemoryStream content = new(BytesUtil.GetRandomBytes());
        content.Seek(0, SeekOrigin.End);
        BinaryMessage binaryMessage = new(content);
        InboundEnvelope inboundEnvelope = new(
            binaryMessage,
            rawMessage,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(inboundEnvelope, _serviceProvider),
            new InvalidOperationException("test"));

        // The message is moved as it was received
        await producer.Received(1).ProduceAsync(Arg.Is<IOutboundEnvelope>(outboundEnvelope => outboundEnvelope.Message == binaryMessage));
        rawMessage.Position.ShouldBe(0);
        content.Position.ShouldBe(0);
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldProduceNonSeekableStreamsAsIs()
    {
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic2"));
        ((ProducerCollection)_serviceProvider.GetRequiredService<IProducerCollection>()).Add(producer);

        IErrorPolicyImplementation policy = new MoveMessageErrorPolicy("topic2").Build(_serviceProvider);

        NonSeekableStream rawMessage = new(BytesUtil.GetRandomBytes());
        NonSeekableStream content = new(BytesUtil.GetRandomBytes());
        BinaryMessage binaryMessage = new(content);
        InboundEnvelope inboundEnvelope = new(
            binaryMessage,
            rawMessage,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(inboundEnvelope, _serviceProvider),
            new InvalidOperationException("test"));

        // The streams that can't be rewound aren't touched
        await producer.Received(1).ProduceAsync(Arg.Is<IOutboundEnvelope>(outboundEnvelope => outboundEnvelope.Message == binaryMessage));
        rawMessage.PositionSetCount.ShouldBe(0);
        content.PositionSetCount.ShouldBe(0);
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldProduceMessage_WhenRawMessageAndBinaryMessageContentAreNull()
    {
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic2"));
        ((ProducerCollection)_serviceProvider.GetRequiredService<IProducerCollection>()).Add(producer);

        IErrorPolicyImplementation policy = new MoveMessageErrorPolicy("topic2").Build(_serviceProvider);

        BinaryMessage binaryMessage = new((Stream?)null);
        InboundEnvelope inboundEnvelope = new(
            binaryMessage,
            null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        bool result = await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(inboundEnvelope, _serviceProvider),
            new InvalidOperationException("test"));

        result.ShouldBeTrue();
        await producer.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope>(outboundEnvelope => outboundEnvelope.Message == binaryMessage));
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldPreserveHeaders()
    {
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic2"));
        ((ProducerCollection)_serviceProvider.GetRequiredService<IProducerCollection>()).Add(producer);

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
            Arg.Is<IOutboundEnvelope>(outboundEnvelope =>
                outboundEnvelope.Headers.GetValue("key1", true) == "value1" &&
                outboundEnvelope.Headers.GetValue("key2", true) == "value2"));
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldTransformHeaders()
    {
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic2"));
        ((ProducerCollection)_serviceProvider.GetRequiredService<IProducerCollection>()).Add(producer);

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
            Arg.Is<IOutboundEnvelope>(outboundEnvelope =>
                outboundEnvelope.Headers.GetValue("error", true) == "InvalidOperationException"));
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldReturnTrue()
    {
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic2"));
        ((ProducerCollection)_serviceProvider.GetRequiredService<IProducerCollection>()).Add(producer);

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

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldCommitOffsetButAbortTransaction()
    {
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic2"));
        ((ProducerCollection)_serviceProvider.GetRequiredService<IProducerCollection>()).Add(producer);

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

    [Fact]
    public async Task HandleErrorAsync_ShouldInvokeCallbacks()
    {
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic2"));
        ((ProducerCollection)_serviceProvider.GetRequiredService<IProducerCollection>()).Add(producer);

        IErrorPolicyImplementation policy = new MoveMessageErrorPolicy("topic2").Build(_serviceProvider);
        InboundEnvelope envelope = new(
            "hey oh!",
            new MemoryStream(),
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());
        InvalidOperationException exception = new("test");

        await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            exception);

        _callback.Invocations.ShouldBe([nameof(IMoveMessageErrorPolicyCallback.OnMovingMessageAsync)]);
        _callback.Envelopes.ShouldBe([envelope]);
        _callback.Exceptions.ShouldBe([exception]);
    }

    private sealed class NonSeekableStream : MemoryStream
    {
        public NonSeekableStream(byte[] buffer)
            : base(buffer)
        {
        }

        public int PositionSetCount { get; private set; }

        public override bool CanSeek => false;

        public override long Position
        {
            get => base.Position;
            set
            {
                PositionSetCount++;
                base.Position = value;
            }
        }
    }
}
