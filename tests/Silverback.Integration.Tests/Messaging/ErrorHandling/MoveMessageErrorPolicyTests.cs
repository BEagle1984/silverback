﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Silverback.Tests.Types;
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
                .AddNullLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>());

            _serviceProvider = services.BuildServiceProvider();

            _broker = _serviceProvider.GetRequiredService<IBroker>();
            _broker.ConnectAsync().Wait();
        }

        [Fact(Skip = "Not yet implemented")]
        public void CanHandle_SingleMessage_TrueReturned()
        {
            throw new NotImplementedException();
        }

        [Fact(Skip = "Not yet implemented")]
        public void CanHandle_Sequence_FalseReturned()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task HandleError_InboundMessage_MessageMoved()
        {
            var policy = ErrorPolicy.Move(TestProducerEndpoint.GetDefault()).Build(_serviceProvider);
            var envelope = new InboundEnvelope(
                "hey oh!",
                new MemoryStream(),
                null,
                new TestOffset(),
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name);

            await policy.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                new InvalidOperationException("test"));
            var producer = (TestProducer)_broker.GetProducer(TestProducerEndpoint.GetDefault());

            producer.ProducedMessages.Should().HaveCount(1);
        }

        [Fact]
        public async Task HandleError_InboundMessage_MessagePreserved()
        {
            var policy = ErrorPolicy.Move(TestProducerEndpoint.GetDefault()).Build(_serviceProvider);

            var message = new TestEventOne { Content = "hey oh!" };
            var headers = new MessageHeaderCollection
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };
            var rawContent = await TestConsumerEndpoint.GetDefault().Serializer
                .SerializeAsync(message, headers, MessageSerializationContext.Empty);
            var envelope = new InboundEnvelope(
                message,
                rawContent,
                headers,
                new TestOffset(),
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name);

            await policy.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                new InvalidOperationException("test"));

            var producer = (TestProducer)_broker.GetProducer(TestProducerEndpoint.GetDefault());

            var producedMessage = producer.ProducedMessages.Last();
            var (deserializedMessage, _) =
                await producedMessage.Endpoint.Serializer.DeserializeAsync(
                    producedMessage.Message,
                    producedMessage.Headers,
                    MessageSerializationContext.Empty);
            deserializedMessage.Should().BeEquivalentTo(envelope.Message);
        }

        [Fact]
        public async Task HandleError_NotDeserializedInboundMessage_MessagePreserved()
        {
            var policy = ErrorPolicy.Move(TestProducerEndpoint.GetDefault()).Build(_serviceProvider);
            var envelope = new InboundEnvelope(
                new MemoryStream(Encoding.UTF8.GetBytes("hey oh!")),
                null,
                new TestOffset(),
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name);

            await policy.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                new InvalidOperationException("test"));

            var producer = (TestProducer)_broker.GetProducer(TestProducerEndpoint.GetDefault());
            var producedMessage = producer.ProducedMessages.Last();

            producedMessage.Message.Should().BeEquivalentTo(producedMessage.Message);
        }

        [Fact]
        public async Task HandleError_InboundMessage_HeadersPreserved()
        {
            var policy = ErrorPolicy.Move(TestProducerEndpoint.GetDefault()).Build(_serviceProvider);
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
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name);

            await policy.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                new InvalidOperationException("test"));

            var producer = (TestProducer)_broker.GetProducer(TestProducerEndpoint.GetDefault());

            producer.ProducedMessages.Last().Headers.Should().Contain(envelope.Headers);
        }

        [Fact]
        public async Task HandleError_WithTransform_MessageTranslated()
        {
            var policy = ErrorPolicy
                .Move(TestProducerEndpoint.GetDefault())
                .Transform((originalEnvelope, ex) => { originalEnvelope.Message = new TestEventTwo(); })
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
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name);

            await policy.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                new InvalidOperationException("test"));

            var producer = (TestProducer)_broker.GetProducer(TestProducerEndpoint.GetDefault());
            var (producedMessage, _) = await producer.Endpoint.Serializer.DeserializeAsync(
                producer.ProducedMessages[0].Message,
                producer.ProducedMessages[0].Headers,
                MessageSerializationContext.Empty);
            producedMessage.Should().BeOfType<TestEventTwo>();
        }

        [Fact]
        public async Task Transform_SingleMessage_HeadersProperlyModified()
        {
            var policy = ErrorPolicy
                .Move(TestProducerEndpoint.GetDefault())
                .Transform((outboundEnvelope, ex) => { outboundEnvelope.Headers.Add("error", ex.GetType().Name); })
                .Build(_serviceProvider);

            var envelope = new InboundEnvelope(
                new MemoryStream(Encoding.UTF8.GetBytes("hey oh!")),
                null,
                new TestOffset(),
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name);
            envelope.Headers.Add("key", "value");

            await policy.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                new InvalidOperationException("test"));

            var producer = (TestProducer)_broker.GetProducer(TestProducerEndpoint.GetDefault());
            var newHeaders = producer.ProducedMessages[0].Headers;
            newHeaders.Should().HaveCount(6); // message-id, message-type, key, traceid, error, source-endpoint
        }

        [Fact]
        public async Task HandleError_SingleMessage_SourceEndpointHeaderIsSet()
        {
            var policy = ErrorPolicy.Move(TestProducerEndpoint.GetDefault()).Build(_serviceProvider);
            var envelope = new InboundEnvelope(
                "hey oh!",
                new MemoryStream(Encoding.UTF8.GetBytes("hey oh!")),
                null,
                new TestOffset(),
                new TestConsumerEndpoint("source-endpoint"),
                "source-endpoint");

            await policy.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                new InvalidOperationException("test"));

            var producer = (TestProducer)_broker.GetProducer(TestProducerEndpoint.GetDefault());

            producer.ProducedMessages.Last()
                .Headers
                .Should().ContainEquivalentOf(
                    new MessageHeader(
                        DefaultMessageHeaders.SourceEndpoint,
                        "source-endpoint"));
        }

        [Fact(Skip = "Not yet implemented")]
        public Task HandleError_Whatever_TrueReturned()
        {
            throw new NotImplementedException();
        }

        [Fact(Skip = "Not yet implemented")]
        public Task HandleError_SingleMessage_OffsetCommitted()
        {
            throw new NotImplementedException();
        }

        [Fact(Skip = "Not yet implemented")]
        public Task HandleError_SingleMessage_TransactionAborted()
        {
            throw new NotImplementedException();
        }
    }
}
