// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Batch;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Connectors
{
    public class InboundConnectorTests
    {
        private readonly TestSubscriber _testSubscriber;
        private readonly WrappedInboundMessageSubscriber _inboundSubscriber;
        private readonly SomeUnhandledMessageSubscriber _someUnhandledMessageSubscriber;
        private readonly IInboundConnector _connector;
        private readonly TestBroker _broker;
        private readonly ErrorPolicyBuilder _errorPolicyBuilder;

        public InboundConnectorTests()
        {
            var services = new ServiceCollection();

            _testSubscriber = new TestSubscriber();
            _inboundSubscriber = new WrappedInboundMessageSubscriber();
            _someUnhandledMessageSubscriber = new SomeUnhandledMessageSubscriber();

            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            services
                .AddSilverback()
                .AddSingletonSubscriber(_testSubscriber)
                .AddSingletonSubscriber(_inboundSubscriber)
                .AddSingletonSubscriber(_someUnhandledMessageSubscriber)
                .WithConnectionTo<TestBroker>(options => options
                    .AddChunkStore<InMemoryChunkStore>());

            var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true
            });
            _broker = (TestBroker)serviceProvider.GetService<IBroker>();
            _connector = new InboundConnector(_broker, serviceProvider);
            _errorPolicyBuilder = new ErrorPolicyBuilder(serviceProvider, NullLoggerFactory.Instance);
        }

        #region Messages Received

        [Fact]
        public async Task Bind_PushMessages_MessagesReceived()
        {
            _connector.Bind(TestEndpoint.Default);
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

           _testSubscriber.ReceivedMessages.Count.Should().Be(5);
        }

        [Fact]
        // Test for issue  #33: messages don't have to be registered with HandleMessagesOfType to be unwrapped and received
        public async Task Bind_PushUnhandledMessages_MessagesUnwrappedAndReceived()
        {
            _connector.Bind(TestEndpoint.Default);
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(new SomeUnhandledMessage { Content = "abc" });
            await consumer.TestPush(new SomeUnhandledMessage { Content = "def" });
            await consumer.TestPush(new SomeUnhandledMessage { Content = "ghi" });

            _someUnhandledMessageSubscriber.ReceivedMessages.Count.Should().Be(3);
        }

        [Fact]
        public async Task Bind_PushMessages_WrappedInboundMessagesReceived()
        {
            _connector.Bind(TestEndpoint.Default);
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            _inboundSubscriber.ReceivedMessages.OfType<InboundMessage<TestEventOne>>().Count().Should().Be(2);
            _inboundSubscriber.ReceivedMessages.OfType<InboundMessage<TestEventTwo>>().Count().Should().Be(3);
        }

        [Fact]
        public async Task Bind_PushMessages_HeadersReceivedWithInboundMessages()
        {
            _connector.Bind(TestEndpoint.Default);
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() }, new[] { new MessageHeader { Key = "key", Value = "value1" } });
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() }, new[] { new MessageHeader { Key = "key", Value = "value2" } });

            var inboundMessages = _inboundSubscriber.ReceivedMessages.OfType<IInboundMessage>();
            var firstMessage = inboundMessages.First();
            firstMessage.Headers.Count.Should().Be(2);
            firstMessage.Headers.Select(h => h.Key).Should().BeEquivalentTo("key", "x-message-type");
            firstMessage.Headers.GetValue("key").Should().Be("value1");
            var secondMessage = inboundMessages.Skip(1).First();
            secondMessage.Headers.Count.Should().Be(2);
            secondMessage.Headers.Select(h => h.Key).Should().BeEquivalentTo("key", "x-message-type");
            secondMessage.Headers.GetValue("key").Should().Be("value2");
        }

        [Fact]
        public async Task Bind_PushMessages_FailedAttemptsReceivedWithInboundMessages()
        {
            _connector.Bind(TestEndpoint.Default);
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() }, new[] { new MessageHeader { Key = MessageHeader.FailedAttemptsKey, Value = "3" } });

            var inboundMessages = _inboundSubscriber.ReceivedMessages.OfType<IInboundMessage>();
            inboundMessages.First().Headers.GetValue<int>(MessageHeader.FailedAttemptsKey).Should().Be(null);
            inboundMessages.Skip(1).First().Headers.GetValue<int>(MessageHeader.FailedAttemptsKey).Should().Be(3);
        }

        [Fact]
        public async Task Bind_PushMessagesInBatch_MessagesReceived()
        {
            _connector.Bind(TestEndpoint.Default, settings: new InboundConnectorSettings
            {
                Batch = new BatchSettings
                {
                    Size = 5
                }
            });

            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            _testSubscriber.ReceivedMessages.Count.Should().Be(0);

            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            _testSubscriber.ReceivedMessages.Count.Should().Be(7);
        }

        [Fact]
        public async Task Bind_PushMessagesInMultipleBatches_MessagesReceived()
        {
            _connector.Bind(TestEndpoint.Default, settings: new InboundConnectorSettings
            {
                Batch = new BatchSettings
                {
                    Size = 5
                }
            });

            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            _testSubscriber.ReceivedMessages.Count.Should().Be(7);
            _testSubscriber.ReceivedMessages.Clear();

            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });

            _testSubscriber.ReceivedMessages.Count.Should().Be(7);
        }

        [Fact]
        public async Task Bind_PushMessagesToMultipleConsumers_MessagesReceived()
        {
            _connector.Bind(TestEndpoint.Default, settings: new InboundConnectorSettings
            {
                Consumers = 5
            });

            _broker.Connect();

            for (int i = 0; i < 3; i++)
            {
                foreach (var consumer in _broker.Consumers)
                {
                    await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
                }
            }

            _testSubscriber.ReceivedMessages.Count.Should().Be(15);
        }

        [Fact]
        public async Task Bind_PushMessagesToMultipleConsumersInBatch_MessagesReceived()
        {
            _connector.Bind(TestEndpoint.Default, settings: new InboundConnectorSettings
            {
                Batch = new BatchSettings
                {
                    Size = 5
                },
                Consumers = 5
            });

            _broker.Connect();

            for (int i = 0; i < 4; i++)
            {
                foreach (var consumer in _broker.Consumers)
                {
                    await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
                }
            }
            _testSubscriber.ReceivedMessages.Count.Should().Be(0);

            foreach (var consumer in _broker.Consumers.Take(3))
            {
                await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            }

            _testSubscriber.ReceivedMessages.Count.Should().Be(7 * 3);

            _testSubscriber.ReceivedMessages.Clear();

            foreach (var consumer in _broker.Consumers.Skip(3))
            {
                await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            }

            _testSubscriber.ReceivedMessages.Count.Should().Be(7 * 2);
        }

        [Fact]
        public async Task Bind_PushMessageChunks_FullMessagesReceived()
        {
            _connector.Bind(TestEndpoint.Default);
            _broker.Connect();

            var buffer = Convert.FromBase64String(
                "eyJDb250ZW50IjoiQSBmdWxsIG1lc3NhZ2UhIiwiSWQiOiI0Mjc1ODMwMi1kOGU5LTQzZjktYjQ3ZS1kN2FjNDFmMmJiMDMifQ==");

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(buffer.Take(40).ToArray(), new[]
            {
                new MessageHeader(MessageHeader.MessageIdKey, "123"),
                new MessageHeader(MessageHeader.ChunkIdKey, "1"),
                new MessageHeader(MessageHeader.ChunksCountKey, "4"),
                new MessageHeader(MessageHeader.MessageTypeKey, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestPush(buffer.Skip(40).Take(40).ToArray(), new[]
            {
                new MessageHeader(MessageHeader.MessageIdKey, "123"),
                new MessageHeader(MessageHeader.ChunkIdKey, "2"),
                new MessageHeader(MessageHeader.ChunksCountKey, "4"),
                new MessageHeader(MessageHeader.MessageTypeKey, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestPush(buffer.Skip(80).Take(40).ToArray(), new[]
            {
                new MessageHeader(MessageHeader.MessageIdKey, "123"),
                new MessageHeader(MessageHeader.ChunkIdKey, "3"),
                new MessageHeader(MessageHeader.ChunksCountKey, "4"),
                new MessageHeader(MessageHeader.MessageTypeKey, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestPush(buffer.Skip(120).ToArray(), new[]
            {
                new MessageHeader(MessageHeader.MessageIdKey, "123"),
                new MessageHeader(MessageHeader.ChunkIdKey, "4"),
                new MessageHeader(MessageHeader.ChunksCountKey, "4"),
                new MessageHeader(MessageHeader.MessageTypeKey, typeof(TestEventOne).AssemblyQualifiedName)
            });

            _testSubscriber.ReceivedMessages.Count.Should().Be(1);
            _testSubscriber.ReceivedMessages.First().As<TestEventOne>().Content.Should().Be("A full message!");
        }

        [Fact]
        public async Task Bind_PushMessageChunksInRandomOrder_FullMessagesReceived()
        {
            _connector.Bind(TestEndpoint.Default);
            _broker.Connect();

            var buffer = Convert.FromBase64String(
                "eyJDb250ZW50IjoiQSBmdWxsIG1lc3NhZ2UhIiwiSWQiOiI0Mjc1ODMwMi1kOGU5LTQzZjktYjQ3ZS1kN2FjNDFmMmJiMDMifQ==");

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(buffer.Take(40).ToArray(), new[]
            {
                new MessageHeader(MessageHeader.MessageIdKey, "123"),
                new MessageHeader(MessageHeader.ChunkIdKey, "1"),
                new MessageHeader(MessageHeader.ChunksCountKey, "4"),
                new MessageHeader(MessageHeader.MessageTypeKey, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestPush(buffer.Skip(120).ToArray(), new[]
            {
                new MessageHeader(MessageHeader.MessageIdKey, "123"),
                new MessageHeader(MessageHeader.ChunkIdKey, "4"),
                new MessageHeader(MessageHeader.ChunksCountKey, "4"),
                new MessageHeader(MessageHeader.MessageTypeKey, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestPush(buffer.Skip(80).Take(40).ToArray(), new[]
            {
                new MessageHeader(MessageHeader.MessageIdKey, "123"),
                new MessageHeader(MessageHeader.ChunkIdKey, "3"),
                new MessageHeader(MessageHeader.ChunksCountKey, "4"),
                new MessageHeader(MessageHeader.MessageTypeKey, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestPush(buffer.Skip(40).Take(40).ToArray(), new[]
            {
                new MessageHeader(MessageHeader.MessageIdKey, "123"),
                new MessageHeader(MessageHeader.ChunkIdKey, "2"),
                new MessageHeader(MessageHeader.ChunksCountKey, "4"),
                new MessageHeader(MessageHeader.MessageTypeKey, typeof(TestEventOne).AssemblyQualifiedName)
            });

            _testSubscriber.ReceivedMessages.Count.Should().Be(1);
            _testSubscriber.ReceivedMessages.First().As<TestEventOne>().Content.Should().Be("A full message!");
        }
        
        [Fact]
        public async Task Bind_PushMessageChunksWithDuplicates_FullMessagesReceived()
        {
            _connector.Bind(TestEndpoint.Default);
            _broker.Connect();

            var buffer = Convert.FromBase64String(
                "eyJDb250ZW50IjoiQSBmdWxsIG1lc3NhZ2UhIiwiSWQiOiI0Mjc1ODMwMi1kOGU5LTQzZjktYjQ3ZS1kN2FjNDFmMmJiMDMifQ==");

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(buffer.Take(40).ToArray(), new[]
            {
                new MessageHeader(MessageHeader.MessageIdKey, "123"),
                new MessageHeader(MessageHeader.ChunkIdKey, "1"),
                new MessageHeader(MessageHeader.ChunksCountKey, "4"),
                new MessageHeader(MessageHeader.MessageTypeKey, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestPush(buffer.Skip(120).ToArray(), new[]
            {
                new MessageHeader(MessageHeader.MessageIdKey, "123"),
                new MessageHeader(MessageHeader.ChunkIdKey, "4"),
                new MessageHeader(MessageHeader.ChunksCountKey, "4"),
                new MessageHeader(MessageHeader.MessageTypeKey, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestPush(buffer.Skip(80).Take(40).ToArray(), new[]
            {
                new MessageHeader(MessageHeader.MessageIdKey, "123"),
                new MessageHeader(MessageHeader.ChunkIdKey, "3"),
                new MessageHeader(MessageHeader.ChunksCountKey, "4"),
                new MessageHeader(MessageHeader.MessageTypeKey, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestPush(buffer.Skip(120).ToArray(), new[]
            {
                new MessageHeader(MessageHeader.MessageIdKey, "123"),
                new MessageHeader(MessageHeader.ChunkIdKey, "4"),
                new MessageHeader(MessageHeader.ChunksCountKey, "4"),
                new MessageHeader(MessageHeader.MessageTypeKey, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestPush(buffer.Skip(80).Take(40).ToArray(), new[]
            {
                new MessageHeader(MessageHeader.MessageIdKey, "123"),
                new MessageHeader(MessageHeader.ChunkIdKey, "3"),
                new MessageHeader(MessageHeader.ChunksCountKey, "4"),
                new MessageHeader(MessageHeader.MessageTypeKey, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestPush(buffer.Skip(40).Take(40).ToArray(), new[]
            {
                new MessageHeader(MessageHeader.MessageIdKey, "123"),
                new MessageHeader(MessageHeader.ChunkIdKey, "2"),
                new MessageHeader(MessageHeader.ChunksCountKey, "4"),
                new MessageHeader(MessageHeader.MessageTypeKey, typeof(TestEventOne).AssemblyQualifiedName)
            });

            _testSubscriber.ReceivedMessages.Count.Should().Be(1);
            _testSubscriber.ReceivedMessages.First().As<TestEventOne>().Content.Should().Be("A full message!");
        }

        #endregion

        #region Acknowledge

        [Fact]
        public async Task Bind_PushMessages_Acknowledged()
        {
            _connector.Bind(TestEndpoint.Default);
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            consumer.AcknowledgeCount.Should().Be(5);
        }

        [Fact]
        public async Task Bind_PushMessagesInBatch_Acknowledged()
        {
            _connector.Bind(TestEndpoint.Default, settings: new InboundConnectorSettings
            {
                Batch = new BatchSettings
                {
                    Size = 5
                }
            });

            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            consumer.AcknowledgeCount.Should().Be(0);

            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            consumer.AcknowledgeCount.Should().Be(5);
        }

        [Fact]
        public async Task Bind_PushMessagesInMultipleBatches_Acknowledged()
        {
            _connector.Bind(TestEndpoint.Default, settings: new InboundConnectorSettings
            {
                Batch = new BatchSettings
                {
                    Size = 5
                }
            });

            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            consumer.AcknowledgeCount.Should().Be(5);

            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });

            consumer.AcknowledgeCount.Should().Be(10);
        }

        [Fact]
        public async Task Bind_PushMessagesToMultipleConsumers_Acknowledged()
        {
            _connector.Bind(TestEndpoint.Default, settings: new InboundConnectorSettings
            {
                Consumers = 5
            });

            _broker.Connect();

            for (int i = 0; i < 3; i++)
            {
                foreach (var consumer in _broker.Consumers)
                {
                    await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
                }
            }

            foreach (var consumer in _broker.Consumers)
            {
                consumer.AcknowledgeCount.Should().Be(3);
            }
        }

        [Fact]
        public async Task Bind_PushMessagesToMultipleConsumersInBatch_Acknowledged()
        {
            _connector.Bind(TestEndpoint.Default, settings: new InboundConnectorSettings
            {
                Batch = new BatchSettings
                {
                    Size = 5
                },
                Consumers = 5
            });

            _broker.Connect();

            for (int i = 0; i < 4; i++)
            {
                foreach (var consumer in _broker.Consumers)
                {
                    await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
                }
            }

            _broker.Consumers.Sum(c => c.AcknowledgeCount).Should().Be(0);

            foreach (var consumer in _broker.Consumers.Take(3))
            {
                await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            }

            _broker.Consumers.Sum(c => c.AcknowledgeCount).Should().Be(15);

            foreach (var consumer in _broker.Consumers.Skip(3))
            {
                await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            }

            _broker.Consumers.Sum(c => c.AcknowledgeCount).Should().Be(25);
        }

        #endregion

        #region Error Policy

        [Fact]
        public async Task Bind_WithRetryErrorPolicy_RetriedAndReceived()
        {
            _testSubscriber.MustFailCount = 3;
            _connector.Bind(TestEndpoint.Default, _errorPolicyBuilder.Retry().MaxFailedAttempts(3));
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(new TestEventOne { Content = "Test", Id = Guid.NewGuid() });

            _testSubscriber.FailCount.Should().Be(3);
            _testSubscriber.ReceivedMessages.Count.Should().Be(4);
        }

        [Fact]
        public async Task Bind_WithRetryErrorPolicyToHandleDeserializerErrors_RetriedAndReceived()
        {
            var testSerializer = new TestSerializer { MustFailCount = 3 };
            _connector.Bind(new TestEndpoint("test")
            {
                Serializer = testSerializer
            }, _errorPolicyBuilder.Retry().MaxFailedAttempts(3));
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(new TestEventOne { Content = "Test", Id = Guid.NewGuid() });

            testSerializer.FailCount.Should().Be(3);
            _testSubscriber.ReceivedMessages.Count.Should().Be(1);
        }

        [Fact]
        public async Task Bind_WithRetryErrorPolicyToHandleDeserializerErrorsInChunkedMessage_RetriedAndReceived()
        {
            var testSerializer = new TestSerializer { MustFailCount = 3 };
            _connector.Bind(new TestEndpoint("test")
            {
                Serializer = testSerializer
            }, _errorPolicyBuilder.Retry().MaxFailedAttempts(3));
            _broker.Connect();

            var buffer = Convert.FromBase64String(
                "eyJDb250ZW50IjoiQSBmdWxsIG1lc3NhZ2UhIiwiSWQiOiI0Mjc1ODMwMi1kOGU5LTQzZjktYjQ3ZS1kN2FjNDFmMmJiMDMifQ==");

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(buffer.Take(40).ToArray(), new[]
            {
                new MessageHeader(MessageHeader.MessageIdKey, "123"),
                new MessageHeader(MessageHeader.ChunkIdKey, "1"),
                new MessageHeader(MessageHeader.ChunksCountKey, "4"),
                new MessageHeader(MessageHeader.MessageTypeKey, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestPush(buffer.Skip(40).Take(40).ToArray(), new[]
            {
                new MessageHeader(MessageHeader.MessageIdKey, "123"),
                new MessageHeader(MessageHeader.ChunkIdKey, "2"),
                new MessageHeader(MessageHeader.ChunksCountKey, "4"),
                new MessageHeader(MessageHeader.MessageTypeKey, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestPush(buffer.Skip(80).Take(40).ToArray(), new[]
            {
                new MessageHeader(MessageHeader.MessageIdKey, "123"),
                new MessageHeader(MessageHeader.ChunkIdKey, "3"),
                new MessageHeader(MessageHeader.ChunksCountKey, "4"),
                new MessageHeader(MessageHeader.MessageTypeKey, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await consumer.TestPush(buffer.Skip(120).ToArray(), new[]
            {
                new MessageHeader(MessageHeader.MessageIdKey, "123"),
                new MessageHeader(MessageHeader.ChunkIdKey, "4"),
                new MessageHeader(MessageHeader.ChunksCountKey, "4"),
                new MessageHeader(MessageHeader.MessageTypeKey, typeof(TestEventOne).AssemblyQualifiedName)
            });

            testSerializer.FailCount.Should().Be(3);
            _testSubscriber.ReceivedMessages.Count.Should().Be(1);
            _testSubscriber.ReceivedMessages.First().As<TestEventOne>().Content.Should().Be("A full message!");
        }

        [Fact]
        public async Task Bind_WithChainedErrorPolicy_RetriedAndMoved()
        {
            _testSubscriber.MustFailCount = 3;
            _connector.Bind(TestEndpoint.Default, _errorPolicyBuilder.Chain(
                _errorPolicyBuilder.Retry().MaxFailedAttempts(1),
                _errorPolicyBuilder.Move(new TestEndpoint("bad"))));
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(new TestEventOne { Content = "Test", Id = Guid.NewGuid() });

            var producer = (TestProducer)_broker.GetProducer(new TestEndpoint("bad"));

            _testSubscriber.FailCount.Should().Be(2);
            _testSubscriber.ReceivedMessages.Count.Should().Be(2);
            producer.ProducedMessages.Count.Should().Be(1);
        }

        [Fact]
        public async Task Bind_WithChainedErrorPolicy_OneAndOnlyOneFailedAttemptsHeaderIsAdded()
        {
            _testSubscriber.MustFailCount = 3;
            _connector.Bind(TestEndpoint.Default, _errorPolicyBuilder.Chain(
                _errorPolicyBuilder.Retry().MaxFailedAttempts(1),
                _errorPolicyBuilder.Move(new TestEndpoint("bad"))));
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(new TestEventOne { Content = "Test", Id = Guid.NewGuid() });

            var producer = (TestProducer)_broker.GetProducer(new TestEndpoint("bad"));

            producer.ProducedMessages.Last().Headers.Count(h => h.Key == MessageHeader.FailedAttemptsKey).Should().Be(1);
        }

        [Fact]
        public async Task Bind_WithRetryErrorPolicy_RetriedAndReceivedInBatch()
        {
            _testSubscriber.MustFailCount = 3;
            _connector.Bind(TestEndpoint.Default,
                _errorPolicyBuilder.Retry().MaxFailedAttempts(3),
                new InboundConnectorSettings
                {
                    Batch = new BatchSettings
                    {
                        Size = 2
                    }
                });
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(new TestEventOne { Content = "Test", Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventOne { Content = "Test", Id = Guid.NewGuid() });

            _testSubscriber.FailCount.Should().Be(3);
            _testSubscriber.ReceivedMessages.OfType<BatchAbortedEvent>().Count().Should().Be(3);
            _testSubscriber.ReceivedMessages.OfType<BatchCompleteEvent>().Count().Should().Be(4);
            _testSubscriber.ReceivedMessages.OfType<BatchProcessedEvent>().Count().Should().Be(1);
            _testSubscriber.ReceivedMessages.OfType<TestEventOne>().Count().Should().Be(5);
        }

        [Fact]
        public async Task Bind_WithRetryErrorPolicyToHandleDeserializerErrors_RetriedAndReceivedInBatch()
        {
            var testSerializer = new TestSerializer { MustFailCount = 3 };
            _connector.Bind(new TestEndpoint("test")
                {
                    Serializer = testSerializer
                },
                _errorPolicyBuilder.Retry().MaxFailedAttempts(3),
                new InboundConnectorSettings
                {
                    Batch = new BatchSettings
                    {
                        Size = 2
                    }
                });
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(new TestEventOne { Content = "Test", Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventOne { Content = "Test", Id = Guid.NewGuid() });

            testSerializer.FailCount.Should().Be(3);
            _testSubscriber.ReceivedMessages.OfType<BatchCompleteEvent>().Count().Should().Be(4);
            _testSubscriber.ReceivedMessages.OfType<BatchAbortedEvent>().Count().Should().Be(3);
            _testSubscriber.ReceivedMessages.OfType<BatchProcessedEvent>().Count().Should().Be(1);
            _testSubscriber.ReceivedMessages.OfType<TestEventOne>().Count().Should().Be(2);
        }
        #endregion
    }
}