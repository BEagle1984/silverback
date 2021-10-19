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
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Inbound.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling
{
    public class MoveMessageErrorPolicyTests
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly IBroker _broker;

        public MoveMessageErrorPolicyTests()
        {
            var services = new ServiceCollection();

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
            var policy = new MoveMessageErrorPolicy(TestProducerConfiguration.GetDefault()).Build(_serviceProvider);
            var envelope = new InboundEnvelope(
                "hey oh!",
                new MemoryStream(),
                null,
                new TestOffset(),
                TestConsumerEndpoint.GetDefault());

            var result = policy.CanHandle(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                new InvalidOperationException("test"));

            result.Should().BeTrue();
        }

        [Fact]
        public void CanHandle_Sequence_FalseReturned()
        {
            var policy = new MoveMessageErrorPolicy(TestProducerConfiguration.GetDefault()).Build(_serviceProvider);
            var endpointConfiguration = new TestConsumerConfiguration("test")
            {
                Batch = new BatchSettings
                {
                    Size = 10
                }
            };
            var envelope = new InboundEnvelope(
                "hey oh!",
                new MemoryStream(),
                null,
                new TestOffset(),
                endpointConfiguration.GetDefaultEndpoint());

            var context = ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider);
            context.SetSequence(new BatchSequence("batch", context), false);

            var result = policy.CanHandle(
                context,
                new InvalidOperationException("test"));

            result.Should().BeFalse();
        }

        [Fact]
        public void CanHandle_RawSequence_FalseReturned()
        {
            var policy = new MoveMessageErrorPolicy(TestProducerConfiguration.GetDefault()).Build(_serviceProvider);
            var envelope = new InboundEnvelope(
                "hey oh!",
                new MemoryStream(),
                null,
                new TestOffset(),
                TestConsumerEndpoint.GetDefault());

            var context = ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider);
            context.SetSequence(new ChunkSequence("123", 5, context), false);

            var result = policy.CanHandle(
                context,
                new InvalidOperationException("test"));

            result.Should().BeFalse();
        }

        [Fact]
        public async Task HandleErrorAsync_InboundMessage_MessageMoved()
        {
            var policy = new MoveMessageErrorPolicy(TestProducerConfiguration.GetDefault()).Build(_serviceProvider);
            var envelope = new InboundEnvelope(
                "hey oh!",
                new MemoryStream(),
                null,
                new TestOffset(),
                TestConsumerEndpoint.GetDefault());

            await policy.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                new InvalidOperationException("test"));
            var producer = (TestProducer)_broker.GetProducer(TestProducerConfiguration.GetDefault());

            producer.ProducedMessages.Should().HaveCount(1);
        }

        [Fact]
        public async Task HandleErrorAsync_InboundMessage_MessagePreserved()
        {
            var policy = new MoveMessageErrorPolicy(TestProducerConfiguration.GetDefault()).Build(_serviceProvider);

            var message = new TestEventOne { Content = "hey oh!" };
            var headers = new MessageHeaderCollection
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };
            var rawContent = await TestConsumerConfiguration.GetDefault().Serializer
                .SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());
            var envelope = new InboundEnvelope(
                message,
                rawContent,
                headers,
                new TestOffset(),
                TestConsumerEndpoint.GetDefault());

            await policy.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                new InvalidOperationException("test"));

            var producer = (TestProducer)_broker.GetProducer(TestProducerConfiguration.GetDefault());

            var producedMessage = producer.ProducedMessages.Last();
            var (deserializedMessage, _) =
                await producedMessage.Endpoint.Configuration.Serializer.DeserializeAsync(
                    new MemoryStream(producedMessage.Message!),
                    producedMessage.Headers,
                    TestConsumerEndpoint.GetDefault());
            deserializedMessage.Should().BeEquivalentTo(envelope.Message);
        }

        [Fact]
        public async Task HandleErrorAsync_NotDeserializedInboundMessage_MessagePreserved()
        {
            var policy = new MoveMessageErrorPolicy(TestProducerConfiguration.GetDefault()).Build(_serviceProvider);
            var envelope = new InboundEnvelope(
                new MemoryStream(Encoding.UTF8.GetBytes("hey oh!")),
                null,
                new TestOffset(),
                TestConsumerEndpoint.GetDefault());

            await policy.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                new InvalidOperationException("test"));

            var producer = (TestProducer)_broker.GetProducer(TestProducerConfiguration.GetDefault());
            var producedMessage = producer.ProducedMessages.Last();

            producedMessage.Message.Should().BeEquivalentTo(producedMessage.Message);
        }

        [Fact]
        public async Task HandleErrorAsync_InboundMessage_HeadersPreserved()
        {
            var policy = new MoveMessageErrorPolicy(TestProducerConfiguration.GetDefault()).Build(_serviceProvider);
            var headers = new MessageHeaderCollection
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };
            var envelope = new InboundEnvelope(
                "hey oh!",
                new MemoryStream(Encoding.UTF8.GetBytes("hey oh!")),
                headers,
                new TestOffset(),
                TestConsumerEndpoint.GetDefault());

            await policy.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                new InvalidOperationException("test"));

            var producer = (TestProducer)_broker.GetProducer(TestProducerConfiguration.GetDefault());

            producer.ProducedMessages.Last().Headers.Should().Contain(envelope.Headers);
        }

        [Fact]
        public async Task HandleErrorAsync_WithTransform_MessageTranslated()
        {
            var policy = new MoveMessageErrorPolicy(TestProducerConfiguration.GetDefault())
                .Transform((originalEnvelope, _) => { originalEnvelope.Message = new TestEventTwo(); })
                .Build(_serviceProvider);

            var rawMessage = new MemoryStream(Encoding.UTF8.GetBytes("hey oh!"));

            var headers = new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(string).AssemblyQualifiedName)
            };

            var envelope = new InboundEnvelope(
                rawMessage,
                headers,
                new TestOffset(),
                TestConsumerEndpoint.GetDefault());

            await policy.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                new InvalidOperationException("test"));

            var producer = (TestProducer)_broker.GetProducer(TestProducerConfiguration.GetDefault());
            var (producedMessage, _) = await producer.Configuration.Serializer.DeserializeAsync(
                new MemoryStream(producer.ProducedMessages[0].Message!),
                producer.ProducedMessages[0].Headers,
                TestConsumerEndpoint.GetDefault());
            producedMessage.Should().BeOfType<TestEventTwo>();
        }

        [Fact]
        public async Task Transform_SingleMessage_HeadersProperlyModified()
        {
            var policy = new MoveMessageErrorPolicy(TestProducerConfiguration.GetDefault())
                .Transform((outboundEnvelope, ex) => { outboundEnvelope.Headers.Add("error", ex.GetType().Name); })
                .Build(_serviceProvider);

            var envelope = new InboundEnvelope(
                new MemoryStream(Encoding.UTF8.GetBytes("hey oh!")),
                null,
                new TestOffset(),
                TestConsumerEndpoint.GetDefault());
            envelope.Headers.Add("key", "value");

            await policy.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                new InvalidOperationException("test"));

            var producer = (TestProducer)_broker.GetProducer(TestProducerConfiguration.GetDefault());
            var newHeaders = producer.ProducedMessages[0].Headers;
            newHeaders.Should().HaveCount(4); // key, error, traceparent, message-id
        }

        [Fact]
        public async Task HandleErrorAsync_SingleMessage_TrueReturned()
        {
            var policy = new MoveMessageErrorPolicy(TestProducerConfiguration.GetDefault()).Build(_serviceProvider);
            var envelope = new InboundEnvelope(
                "hey oh!",
                new MemoryStream(Encoding.UTF8.GetBytes("hey oh!")),
                null,
                new TestOffset(),
                new TestConsumerConfiguration("source-endpoint").GetDefaultEndpoint());

            var result = await policy.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                new InvalidOperationException("test"));

            result.Should().BeTrue();
        }

        [Fact]
        public async Task HandleErrorAsync_Whatever_ConsumerCommittedButTransactionAborted()
        {
            var policy = new MoveMessageErrorPolicy(TestProducerConfiguration.GetDefault()).Build(_serviceProvider);
            var envelope = new InboundEnvelope(
                "hey oh!",
                new MemoryStream(Encoding.UTF8.GetBytes("hey oh!")),
                null,
                new TestOffset(),
                new TestConsumerConfiguration("source-endpoint").GetDefaultEndpoint());

            var transactionManager = Substitute.For<IConsumerTransactionManager>();

            await policy.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider, transactionManager),
                new InvalidOperationException("test"));

            await transactionManager.Received(1).RollbackAsync(Arg.Any<InvalidOperationException>(), true);
        }
    }
}
