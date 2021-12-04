// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Inbound.Transaction;
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
    private readonly IServiceProvider _serviceProvider;

    private readonly IBroker _broker;

    public MoveMessageErrorPolicyTests()
    {
        ServiceCollection services = new();

        services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddLoggerSubstitute()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>());

        _serviceProvider = services.BuildServiceProvider();

        _broker = _serviceProvider.GetRequiredService<IBroker>();
        _broker.ConnectAsync().Wait();
    }

    [Fact]
    public void CanHandle_SingleMessage_TrueReturned()
    {
        IErrorPolicyImplementation policy = new MoveMessageErrorPolicy(TestProducerConfiguration.GetDefault()).Build(_serviceProvider);
        InboundEnvelope envelope = new(
            "hey oh!",
            new MemoryStream(),
            null,
            new TestOffset(),
            TestConsumerEndpoint.GetDefault());

        bool result = policy.CanHandle(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            new InvalidOperationException("test"));

        result.Should().BeTrue();
    }

    [Fact]
    public void CanHandle_Sequence_FalseReturned()
    {
        IErrorPolicyImplementation policy = new MoveMessageErrorPolicy(TestProducerConfiguration.GetDefault()).Build(_serviceProvider);
        TestConsumerConfiguration endpointConfiguration = new("test")
        {
            Batch = new BatchSettings
            {
                Size = 10
            }
        };
        InboundEnvelope envelope = new(
            "hey oh!",
            new MemoryStream(),
            null,
            new TestOffset(),
            endpointConfiguration.GetDefaultEndpoint());

        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider);
        context.SetSequence(new BatchSequence("batch", context), false);

        bool result = policy.CanHandle(
            context,
            new InvalidOperationException("test"));

        result.Should().BeFalse();
    }

    [Fact]
    public void CanHandle_RawSequence_FalseReturned()
    {
        IErrorPolicyImplementation policy = new MoveMessageErrorPolicy(TestProducerConfiguration.GetDefault()).Build(_serviceProvider);
        InboundEnvelope envelope = new(
            "hey oh!",
            new MemoryStream(),
            null,
            new TestOffset(),
            TestConsumerEndpoint.GetDefault());

        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider);
        context.SetSequence(new ChunkSequence("123", 5, context), false);

        bool result = policy.CanHandle(
            context,
            new InvalidOperationException("test"));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleErrorAsync_InboundMessage_MessageMoved()
    {
        IErrorPolicyImplementation policy = new MoveMessageErrorPolicy(TestProducerConfiguration.GetDefault()).Build(_serviceProvider);
        InboundEnvelope envelope = new(
            "hey oh!",
            new MemoryStream(),
            null,
            new TestOffset(),
            TestConsumerEndpoint.GetDefault());

        await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            new InvalidOperationException("test"));
        TestProducer producer = (TestProducer)_broker.GetProducer(TestProducerConfiguration.GetDefault());

        producer.ProducedMessages.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleErrorAsync_InboundMessage_MessagePreserved()
    {
        IErrorPolicyImplementation policy = new MoveMessageErrorPolicy(TestProducerConfiguration.GetDefault()).Build(_serviceProvider);

        TestEventOne message = new() { Content = "hey oh!" };
        MessageHeaderCollection headers = new()
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };
        Stream? rawContent = await TestConsumerConfiguration.GetDefault().Serializer
            .SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());
        InboundEnvelope envelope = new(
            message,
            rawContent,
            headers,
            new TestOffset(),
            TestConsumerEndpoint.GetDefault());

        await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            new InvalidOperationException("test"));

        TestProducer producer = (TestProducer)_broker.GetProducer(TestProducerConfiguration.GetDefault());

        ProducedMessage producedMessage = producer.ProducedMessages.Last();
        (object? deserializedMessage, _) =
            await producedMessage.Endpoint.Configuration.Serializer.DeserializeAsync(
                new MemoryStream(producedMessage.Message!),
                producedMessage.Headers,
                TestConsumerEndpoint.GetDefault());
        deserializedMessage.Should().BeEquivalentTo(envelope.Message);
    }

    [Fact]
    public async Task HandleErrorAsync_NotDeserializedInboundMessage_MessagePreserved()
    {
        IErrorPolicyImplementation policy = new MoveMessageErrorPolicy(TestProducerConfiguration.GetDefault()).Build(_serviceProvider);
        InboundEnvelope envelope = new(
            new MemoryStream(Encoding.UTF8.GetBytes("hey oh!")),
            null,
            new TestOffset(),
            TestConsumerEndpoint.GetDefault());

        await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            new InvalidOperationException("test"));

        TestProducer producer = (TestProducer)_broker.GetProducer(TestProducerConfiguration.GetDefault());
        ProducedMessage producedMessage = producer.ProducedMessages.Last();

        producedMessage.Message.Should().BeEquivalentTo(producedMessage.Message);
    }

    [Fact]
    public async Task HandleErrorAsync_InboundMessage_HeadersPreserved()
    {
        IErrorPolicyImplementation policy = new MoveMessageErrorPolicy(TestProducerConfiguration.GetDefault()).Build(_serviceProvider);
        MessageHeaderCollection headers = new()
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };
        InboundEnvelope envelope = new(
            "hey oh!",
            new MemoryStream(Encoding.UTF8.GetBytes("hey oh!")),
            headers,
            new TestOffset(),
            TestConsumerEndpoint.GetDefault());

        await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            new InvalidOperationException("test"));

        TestProducer producer = (TestProducer)_broker.GetProducer(TestProducerConfiguration.GetDefault());

        producer.ProducedMessages.Last().Headers.Should().Contain(envelope.Headers);
    }

    [Fact]
    public async Task HandleErrorAsync_WithTransform_MessageTranslated()
    {
        IErrorPolicyImplementation policy = new MoveMessageErrorPolicy(TestProducerConfiguration.GetDefault())
            .Transform(
                (originalEnvelope, _) =>
                {
                    originalEnvelope.Message = new TestEventTwo();
                })
            .Build(_serviceProvider);

        MemoryStream rawMessage = new(Encoding.UTF8.GetBytes("hey oh!"));

        MessageHeader[] headers =
        {
            new(DefaultMessageHeaders.MessageType, typeof(string).AssemblyQualifiedName)
        };

        InboundEnvelope envelope = new(
            rawMessage,
            headers,
            new TestOffset(),
            TestConsumerEndpoint.GetDefault());

        await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            new InvalidOperationException("test"));

        TestProducer producer = (TestProducer)_broker.GetProducer(TestProducerConfiguration.GetDefault());
        (object? producedMessage, _) = await producer.Configuration.Serializer.DeserializeAsync(
            new MemoryStream(producer.ProducedMessages[0].Message!),
            producer.ProducedMessages[0].Headers,
            TestConsumerEndpoint.GetDefault());
        producedMessage.Should().BeOfType<TestEventTwo>();
    }

    [Fact]
    public async Task Transform_SingleMessage_HeadersProperlyModified()
    {
        IErrorPolicyImplementation policy = new MoveMessageErrorPolicy(TestProducerConfiguration.GetDefault())
            .Transform(
                (outboundEnvelope, ex) =>
                {
                    outboundEnvelope.Headers.Add("error", ex.GetType().Name);
                })
            .Build(_serviceProvider);

        InboundEnvelope envelope = new(
            new MemoryStream(Encoding.UTF8.GetBytes("hey oh!")),
            null,
            new TestOffset(),
            TestConsumerEndpoint.GetDefault());
        envelope.Headers.Add("key", "value");

        await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            new InvalidOperationException("test"));

        TestProducer producer = (TestProducer)_broker.GetProducer(TestProducerConfiguration.GetDefault());
        MessageHeaderCollection newHeaders = producer.ProducedMessages[0].Headers;
        newHeaders.Should().HaveCount(4); // key, error, traceparent, message-id
    }

    [Fact]
    public async Task HandleErrorAsync_SingleMessage_TrueReturned()
    {
        IErrorPolicyImplementation policy = new MoveMessageErrorPolicy(TestProducerConfiguration.GetDefault()).Build(_serviceProvider);
        InboundEnvelope envelope = new(
            "hey oh!",
            new MemoryStream(Encoding.UTF8.GetBytes("hey oh!")),
            null,
            new TestOffset(),
            new TestConsumerConfiguration("source-endpoint").GetDefaultEndpoint());

        bool result = await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            new InvalidOperationException("test"));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HandleErrorAsync_Whatever_ConsumerCommittedButTransactionAborted()
    {
        IErrorPolicyImplementation policy = new MoveMessageErrorPolicy(TestProducerConfiguration.GetDefault()).Build(_serviceProvider);
        InboundEnvelope envelope = new(
            "hey oh!",
            new MemoryStream(Encoding.UTF8.GetBytes("hey oh!")),
            null,
            new TestOffset(),
            new TestConsumerConfiguration("source-endpoint").GetDefaultEndpoint());

        IConsumerTransactionManager? transactionManager = Substitute.For<IConsumerTransactionManager>();

        await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider, transactionManager),
            new InvalidOperationException("test"));

        await transactionManager.Received(1).RollbackAsync(Arg.Any<InvalidOperationException>(), true);
    }
}
