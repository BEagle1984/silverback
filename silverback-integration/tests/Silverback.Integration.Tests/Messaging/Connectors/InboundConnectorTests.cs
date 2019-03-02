// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Batch;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Messaging.Connectors
{
    public class InboundConnectorTests
    {
        private readonly TestSubscriber _testSubscriber;
        private readonly IInboundConnector _connector;
        private readonly TestBroker _broker;
        private readonly ErrorPolicyBuilder _errorPolicyBuilder;

        public InboundConnectorTests()
        {
            var services = new ServiceCollection();

            _testSubscriber = new TestSubscriber();
            services.AddSingleton<ISubscriber>(_testSubscriber);

            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            services.AddBus();

            services.AddBroker<TestBroker>(options => options.AddChunkStore(_ => new InMemoryChunkStore()));

            var serviceProvider = services.BuildServiceProvider();
            _broker = (TestBroker)serviceProvider.GetService<IBroker>();
            _connector = new InboundConnector(_broker, serviceProvider, new NullLogger<InboundConnector>());
            _errorPolicyBuilder = new ErrorPolicyBuilder(serviceProvider, NullLoggerFactory.Instance);
        }

        #region Messages Received

        [Fact]
        public void Bind_PushMessages_MessagesReceived()
        {
            _connector.Bind(TestEndpoint.Default);
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

           _testSubscriber.ReceivedMessages.Count.Should().Be(5);
        }

        [Fact]
        public void Bind_PushMessagesInBatch_MessagesReceived()
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
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            _testSubscriber.ReceivedMessages.Count.Should().Be(0);

            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            _testSubscriber.ReceivedMessages.Count.Should().Be(7);
        }

        [Fact]
        public void Bind_PushMessagesInMultipleBatches_MessagesReceived()
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
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            _testSubscriber.ReceivedMessages.Count.Should().Be(7);
            _testSubscriber.ReceivedMessages.Clear();

            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });

            _testSubscriber.ReceivedMessages.Count.Should().Be(7);
        }

        [Fact]
        public void Bind_PushMessagesToMultipleConsumers_MessagesReceived()
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
                    consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
                }
            }

            _testSubscriber.ReceivedMessages.Count.Should().Be(15);
        }

        [Fact]
        public void Bind_PushMessagesToMultipleConsumersInBatch_MessagesReceived()
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
                    consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
                }
            }
           _testSubscriber.ReceivedMessages.Count.Should().Be(0);

            foreach (var consumer in _broker.Consumers.Take(3))
            {
                consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            }

            _testSubscriber.ReceivedMessages.Count.Should().Be(7 * 3);

            _testSubscriber.ReceivedMessages.Clear();

            foreach (var consumer in _broker.Consumers.Skip(3))
            {
                consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            }

            _testSubscriber.ReceivedMessages.Count.Should().Be(7 * 2);
        }

        [Fact]
        public void Bind_PushMessageChunks_FullMessagesReceived()
        {
            _connector.Bind(TestEndpoint.Default);
            _broker.Connect();

            var buffer = Convert.FromBase64String(
                "eyIkdHlwZSI6IlNpbHZlcmJhY2suVGVzdHMuVGVzdFR5cGVzLkRvbWFpbi5UZXN0RXZlbnRPbmUsIFNp" +
                "bHZlcmJhY2suSW50ZWdyYXRpb24uVGVzdHMiLCJDb250ZW50IjoiQSBmdWxsIG1lc3NhZ2UhIiwiSWQi" +
                "OiI0Mjc1ODMwMi1kOGU5LTQzZjktYjQ3ZS1kN2FjNDFmMmJiMDMifQ==");

            var consumer = _broker.Consumers.First();
            consumer.TestPush(new MessageChunk
            {
                 MessageId = Guid.NewGuid(),
                 OriginalMessageId = "123",
                 ChunkId = 1,
                 ChunksCount = 4,
                 Content = buffer.Take(40).ToArray()
            });
            consumer.TestPush(new MessageChunk
            {
                MessageId = Guid.NewGuid(),
                OriginalMessageId = "123",
                ChunkId = 2,
                ChunksCount = 4,
                Content = buffer.Skip(40).Take(40).ToArray()
            });
            consumer.TestPush(new MessageChunk
            {
                MessageId = Guid.NewGuid(),
                OriginalMessageId = "123",
                ChunkId = 3,
                ChunksCount = 4,
                Content = buffer.Skip(80).Take(40).ToArray()
            });
            consumer.TestPush(new MessageChunk
            {
                MessageId = Guid.NewGuid(),
                OriginalMessageId = "123",
                ChunkId = 4,
                ChunksCount = 4,
                Content = buffer.Skip(120).ToArray()
            });

            _testSubscriber.ReceivedMessages.Count.Should().Be(1);
            _testSubscriber.ReceivedMessages.First().As<TestEventOne>().Content.Should().Be("A full message!");
        }

        [Fact]
        public void Bind_PushMessageChunksInRandomOrder_FullMessagesReceived()
        {
            _connector.Bind(TestEndpoint.Default);
            _broker.Connect();

            var buffer = Convert.FromBase64String(
                "eyIkdHlwZSI6IlNpbHZlcmJhY2suVGVzdHMuVGVzdFR5cGVzLkRvbWFpbi5UZXN0RXZlbnRPbmUsIFNp" +
                "bHZlcmJhY2suSW50ZWdyYXRpb24uVGVzdHMiLCJDb250ZW50IjoiQSBmdWxsIG1lc3NhZ2UhIiwiSWQi" +
                "OiI0Mjc1ODMwMi1kOGU5LTQzZjktYjQ3ZS1kN2FjNDFmMmJiMDMifQ==");

            var consumer = _broker.Consumers.First();
            consumer.TestPush(new MessageChunk
            {
                MessageId = Guid.NewGuid(),
                OriginalMessageId = "123",
                ChunkId = 1,
                ChunksCount = 4,
                Content = buffer.Take(40).ToArray()
            });
            consumer.TestPush(new MessageChunk
            {
                MessageId = Guid.NewGuid(),
                OriginalMessageId = "123",
                ChunkId = 4,
                ChunksCount = 4,
                Content = buffer.Skip(120).ToArray()
            });
            consumer.TestPush(new MessageChunk
            {
                MessageId = Guid.NewGuid(),
                OriginalMessageId = "123",
                ChunkId = 3,
                ChunksCount = 4,
                Content = buffer.Skip(80).Take(40).ToArray()
            });
            consumer.TestPush(new MessageChunk
            {
                MessageId = Guid.NewGuid(),
                OriginalMessageId = "123",
                ChunkId = 2,
                ChunksCount = 4,
                Content = buffer.Skip(40).Take(40).ToArray()
            });

            _testSubscriber.ReceivedMessages.Count.Should().Be(1);
            _testSubscriber.ReceivedMessages.First().As<TestEventOne>().Content.Should().Be("A full message!");
        }
        
        [Fact]
        public void Bind_PushMessageChunksWithDuplicates_FullMessagesReceived()
        {
            _connector.Bind(TestEndpoint.Default);
            _broker.Connect();

            var buffer = Convert.FromBase64String(
                "eyIkdHlwZSI6IlNpbHZlcmJhY2suVGVzdHMuVGVzdFR5cGVzLkRvbWFpbi5UZXN0RXZlbnRPbmUsIFNp" +
                "bHZlcmJhY2suSW50ZWdyYXRpb24uVGVzdHMiLCJDb250ZW50IjoiQSBmdWxsIG1lc3NhZ2UhIiwiSWQi" +
                "OiI0Mjc1ODMwMi1kOGU5LTQzZjktYjQ3ZS1kN2FjNDFmMmJiMDMifQ==");

            var consumer = _broker.Consumers.First();
            consumer.TestPush(new MessageChunk
            {
                MessageId = Guid.NewGuid(),
                OriginalMessageId = "123",
                ChunkId = 1,
                ChunksCount = 4,
                Content = buffer.Take(40).ToArray()
            });
            consumer.TestPush(new MessageChunk
            {
                MessageId = Guid.NewGuid(),
                OriginalMessageId = "123",
                ChunkId = 2,
                ChunksCount = 4,
                Content = buffer.Skip(40).Take(40).ToArray()
            });
            consumer.TestPush(new MessageChunk
            {
                MessageId = Guid.NewGuid(),
                OriginalMessageId = "123",
                ChunkId = 3,
                ChunksCount = 4,
                Content = buffer.Skip(80).Take(40).ToArray()
            });
            consumer.TestPush(new MessageChunk
            {
                MessageId = Guid.NewGuid(),
                OriginalMessageId = "123",
                ChunkId = 2,
                ChunksCount = 4,
                Content = buffer.Skip(40).Take(40).ToArray()
            });
            consumer.TestPush(new MessageChunk
            {
                MessageId = Guid.NewGuid(),
                OriginalMessageId = "123",
                ChunkId = 1,
                ChunksCount = 4,
                Content = buffer.Take(40).ToArray()
            });
            consumer.TestPush(new MessageChunk
            {
                MessageId = Guid.NewGuid(),
                OriginalMessageId = "123",
                ChunkId = 4,
                ChunksCount = 4,
                Content = buffer.Skip(120).ToArray()
            });

            _testSubscriber.ReceivedMessages.Count.Should().Be(1);
            _testSubscriber.ReceivedMessages.First().As<TestEventOne>().Content.Should().Be("A full message!");
        }

        #endregion

        #region Acknowledge

        [Fact]
        public void Bind_PushMessages_Acknowledged()
        {
            _connector.Bind(TestEndpoint.Default);
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            consumer.AcknowledgeCount.Should().Be(5);
        }

        [Fact]
        public void Bind_PushMessagesInBatch_Acknowledged()
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
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            consumer.AcknowledgeCount.Should().Be(0);

            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            consumer.AcknowledgeCount.Should().Be(5);
        }

        [Fact]
        public void Bind_PushMessagesInMultipleBatches_Acknowledged()
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
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            consumer.AcknowledgeCount.Should().Be(5);

            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });

            consumer.AcknowledgeCount.Should().Be(10);
        }

        [Fact]
        public void Bind_PushMessagesToMultipleConsumers_Acknowledged()
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
                    consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
                }
            }

            foreach (var consumer in _broker.Consumers)
            {
                consumer.AcknowledgeCount.Should().Be(3);
            }
        }

        [Fact]
        public void Bind_PushMessagesToMultipleConsumersInBatch_Acknowledged()
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
                    consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
                }
            }

            _broker.Consumers.Sum(c => c.AcknowledgeCount).Should().Be(0);

            foreach (var consumer in _broker.Consumers.Take(3))
            {
                consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            }

            _broker.Consumers.Sum(c => c.AcknowledgeCount).Should().Be(15);

            foreach (var consumer in _broker.Consumers.Skip(3))
            {
                consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            }

            _broker.Consumers.Sum(c => c.AcknowledgeCount).Should().Be(25);
        }

        #endregion

        #region Error Policy

        [Fact]
        public void Bind_WithRetryErrorPolicy_RetriedAndReceived()
        {
            _testSubscriber.MustFailCount = 3;
            _connector.Bind(TestEndpoint.Default, _errorPolicyBuilder.Retry().MaxFailedAttempts(3));
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            consumer.TestPush(new TestEventOne { Content = "Test", Id = Guid.NewGuid() });

            _testSubscriber.FailCount.Should().Be(3);
            _testSubscriber.ReceivedMessages.Count.Should().Be(4);
        }

        [Fact]
        public void Bind_WithChainedErrorPolicy_RetriedAndMoved()
        {
            _testSubscriber.MustFailCount = 3;
            _connector.Bind(TestEndpoint.Default, _errorPolicyBuilder.Chain(
                _errorPolicyBuilder.Retry().MaxFailedAttempts(1),
                _errorPolicyBuilder.Move(new TestEndpoint("bad"))));
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            consumer.TestPush(new TestEventOne { Content = "Test", Id = Guid.NewGuid() });

            var producer = (TestProducer)_broker.GetProducer(new TestEndpoint("bad"));

            _testSubscriber.FailCount.Should().Be(2);
            _testSubscriber.ReceivedMessages.Count.Should().Be(2);
            producer.ProducedMessages.Count.Should().Be(1);
        }

        [Fact]
        public void Bind_WithRetryErrorPolicy_RetriedAndReceivedInBatch()
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
            consumer.TestPush(new TestEventOne { Content = "Test", Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventOne { Content = "Test", Id = Guid.NewGuid() });

            _testSubscriber.FailCount.Should().Be(3);
            _testSubscriber.ReceivedMessages.OfType<BatchAbortedEvent>().Count().Should().Be(3);
            _testSubscriber.ReceivedMessages.OfType<BatchCompleteEvent>().Count().Should().Be(4);
            _testSubscriber.ReceivedMessages.OfType<BatchProcessedEvent>().Count().Should().Be(1);
            _testSubscriber.ReceivedMessages.OfType<TestEventOne>().Count().Should().Be(5);
        }

        #endregion
    }
}