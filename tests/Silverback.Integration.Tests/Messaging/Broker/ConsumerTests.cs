// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker
{
    public class ConsumerTests
    {
        private readonly TestBroker _broker;
        private readonly SilverbackEventsSubscriber _silverbackEventsSubscriber;

        public ConsumerTests()
        {
            var services = new ServiceCollection();

            services
                .AddNullLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options
                    .AddBroker<TestBroker>()
                    .AddChunkStore<InMemoryChunkStore>())
                .AddSingletonSubscriber<SilverbackEventsSubscriber>();

            var serviceProvider = services.BuildServiceProvider();

            _broker = serviceProvider.GetRequiredService<TestBroker>();
            _silverbackEventsSubscriber = serviceProvider.GetRequiredService<SilverbackEventsSubscriber>();
        }

        [Fact]
        public async Task HandleMessage_SomeMessages_MessagesReceived()
        {
            var envelopes = new List<IRawInboundEnvelope>();
            var consumer = (TestConsumer) _broker.GetConsumer(TestConsumerEndpoint.GetDefault());
            consumer.Received += (_, args) =>
            {
                envelopes.AddRange(args.Envelopes);
                return Task.CompletedTask;
            };
            _broker.Connect();

            await consumer.TestHandleMessage(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestHandleMessage(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestHandleMessage(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestHandleMessage(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestHandleMessage(new TestEventTwo { Id = Guid.NewGuid() });

            envelopes.Count.Should().Be(5);
        }

        [Fact]
        public async Task HandleMessage_MessageCorrectlyProcessed_ConsumingCompletedEventPublished()
        {
            var consumer = (TestConsumer) _broker.GetConsumer(TestConsumerEndpoint.GetDefault());
            consumer.Received += (_, args) => Task.CompletedTask;
            _broker.Connect();

            await consumer.TestHandleMessage(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestHandleMessage(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestHandleMessage(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestHandleMessage(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestHandleMessage(new TestEventTwo { Id = Guid.NewGuid() });

            _silverbackEventsSubscriber.ReceivedEvents.OfType<ConsumingCompletedEvent>()
                .Count().Should().Be(5);
        }

        [Fact]
        public async Task HandleMessage_SomeMessagesFailToBeProcessed_ConsumingCompletedAndAbortedEventsPublished()
        {
            var consumer = (TestConsumer) _broker.GetConsumer(TestConsumerEndpoint.GetDefault());
            consumer.Received += (_, args) =>
            {
                if (args.Envelopes.First() is IInboundEnvelope<TestEventOne>)
                    throw new Exception();

                return Task.CompletedTask;
            };
            _broker.Connect();

            try
            {
                await consumer.TestHandleMessage(new TestEventOne { Id = Guid.NewGuid() });
            }
            catch (Exception)
            {
                // ignored
            }

            await consumer.TestHandleMessage(new TestEventTwo { Id = Guid.NewGuid() });

            try
            {
                await consumer.TestHandleMessage(new TestEventOne { Id = Guid.NewGuid() });
            }
            catch (Exception)
            {
                // ignored
            }

            await consumer.TestHandleMessage(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestHandleMessage(new TestEventTwo { Id = Guid.NewGuid() });

            _silverbackEventsSubscriber.ReceivedEvents.OfType<ConsumingCompletedEvent>()
                .Count().Should().Be(3);
            _silverbackEventsSubscriber.ReceivedEvents.OfType<ConsumingAbortedEvent>()
                .Count().Should().Be(2);
        }

        [Fact]
        public async Task HandleMessage_SomeMessages_HeadersReceivedWithInboundMessages()
        {
            var envelopes = new List<IRawInboundEnvelope>();
            var consumer = (TestConsumer) _broker.GetConsumer(TestConsumerEndpoint.GetDefault());
            consumer.Received += (_, args) =>
            {
                envelopes.AddRange(args.Envelopes);
                return Task.CompletedTask;
            };
            _broker.Connect();

            await consumer.TestHandleMessage(new TestEventOne { Id = Guid.NewGuid() },
                new[] { new MessageHeader { Key = "key", Value = "value1" } });
            await consumer.TestHandleMessage(new TestEventOne { Id = Guid.NewGuid() },
                new[] { new MessageHeader { Key = "key", Value = "value2" } });

            var firstMessage = envelopes.First();
            firstMessage.Headers.Count.Should().Be(2);
            firstMessage.Headers.Select(h => h.Key).Should().BeEquivalentTo("key", "x-message-type");
            firstMessage.Headers.GetValue("key").Should().Be("value1");
            var secondMessage = envelopes.Skip(1).First();
            secondMessage.Headers.Count.Should().Be(2);
            secondMessage.Headers.Select(h => h.Key).Should().BeEquivalentTo("key", "x-message-type");
            secondMessage.Headers.GetValue("key").Should().Be("value2");
        }

        [Fact]
        public async Task HandleMessage_SomeMessages_FailedAttemptsReceivedWithInboundMessages()
        {
            var envelopes = new List<IRawInboundEnvelope>();
            var consumer = (TestConsumer) _broker.GetConsumer(TestConsumerEndpoint.GetDefault());
            consumer.Received += (_, args) =>
            {
                envelopes.AddRange(args.Envelopes);
                return Task.CompletedTask;
            };
            _broker.Connect();

            await consumer.TestHandleMessage(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestHandleMessage(new TestEventOne { Id = Guid.NewGuid() },
                new[] { new MessageHeader { Key = DefaultMessageHeaders.FailedAttempts, Value = "3" } });

            envelopes.First().Headers.GetValue<int>(DefaultMessageHeaders.FailedAttempts).Should().Be(null);
            envelopes.Skip(1).First().Headers.GetValue<int>(DefaultMessageHeaders.FailedAttempts).Should().Be(3);
        }

        [Fact]
        public async Task HandleMessage_ChunkedMessage_FullMessagesReceived()
        {
            var originalPayload = Convert.FromBase64String(
                "eyJDb250ZW50IjoiQSBmdWxsIG1lc3NhZ2UhIiwiSWQiOiI0Mjc1ODMwMi1kOGU5LTQzZjktYjQ3ZS1kN2FjNDFmMmJiMDMifQ==");

            var envelopes = new List<IRawInboundEnvelope>();
            var consumer = (TestConsumer) _broker.GetConsumer(TestConsumerEndpoint.GetDefault());
            consumer.Received += (_, args) =>
            {
                envelopes.AddRange(args.Envelopes);
                return Task.CompletedTask;
            };
            _broker.Connect();

            await consumer.TestConsume(originalPayload.Take(40).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkId, "1"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestConsume(originalPayload.Skip(40).Take(40).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkId, "2"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestConsume(originalPayload.Skip(80).Take(40).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkId, "3"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestConsume(originalPayload.Skip(120).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkId, "4"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });

            envelopes.Count.Should().Be(1);
            envelopes.First().Should().BeOfType<InboundEnvelope<TestEventOne>>();
            envelopes.First().As<InboundEnvelope<TestEventOne>>().Message.Content.Should().Be("A full message!");
        }

        [Fact]
        public async Task HandleMessage_MessageChunksInRandomOrder_FullMessageReceived()
        {
            var originalPayload = Convert.FromBase64String(
                "eyJDb250ZW50IjoiQSBmdWxsIG1lc3NhZ2UhIiwiSWQiOiI0Mjc1ODMwMi1kOGU5LTQzZjktYjQ3ZS1kN2FjNDFmMmJiMDMifQ==");

            var envelopes = new List<IRawInboundEnvelope>();
            var consumer = (TestConsumer) _broker.GetConsumer(TestConsumerEndpoint.GetDefault());
            consumer.Received += (_, args) =>
            {
                envelopes.AddRange(args.Envelopes);
                return Task.CompletedTask;
            };
            _broker.Connect();

            await consumer.TestConsume(originalPayload.Skip(80).Take(40).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkId, "3"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestConsume(originalPayload.Take(40).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkId, "1"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestConsume(originalPayload.Skip(120).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkId, "4"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestConsume(originalPayload.Skip(40).Take(40).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkId, "2"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });

            envelopes.Count.Should().Be(1);
            envelopes.First().Should().BeOfType<InboundEnvelope<TestEventOne>>();
            envelopes.First().As<InboundEnvelope<TestEventOne>>().Message.Content.Should().Be("A full message!");
        }

        [Fact]
        public async Task HandleMessage_MessageChunksWithDuplicates_FullMessageReceived()
        {
            var originalPayload = Convert.FromBase64String(
                "eyJDb250ZW50IjoiQSBmdWxsIG1lc3NhZ2UhIiwiSWQiOiI0Mjc1ODMwMi1kOGU5LTQzZjktYjQ3ZS1kN2FjNDFmMmJiMDMifQ==");

            var envelopes = new List<IRawInboundEnvelope>();
            var consumer = (TestConsumer) _broker.GetConsumer(TestConsumerEndpoint.GetDefault());
            consumer.Received += (_, args) =>
            {
                envelopes.AddRange(args.Envelopes);
                return Task.CompletedTask;
            };
            _broker.Connect();

            await consumer.TestConsume(originalPayload.Take(40).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkId, "1"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestConsume(originalPayload.Skip(80).Take(40).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkId, "3"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestConsume(originalPayload.Skip(40).Take(40).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkId, "2"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestConsume(originalPayload.Skip(40).Take(40).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkId, "2"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestConsume(originalPayload.Skip(80).Take(40).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkId, "3"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestConsume(originalPayload.Skip(120).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkId, "4"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestConsume(originalPayload.Skip(80).Take(40).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkId, "3"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });

            envelopes.Count.Should().Be(1);
            envelopes.First().Should().BeOfType<InboundEnvelope<TestEventOne>>();
            envelopes.First().As<InboundEnvelope<TestEventOne>>().Message.Content.Should().Be("A full message!");
        }
    }
}