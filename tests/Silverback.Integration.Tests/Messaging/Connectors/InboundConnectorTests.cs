// TODO: Migrate or delete tests

// // Copyright (c) 2020 Sergio Aquilini
// // This code is licensed under MIT license (see LICENSE file for details)
//
// using System;
// using System.Linq;
// using System.Threading.Tasks;
// using FluentAssertions;
// using Microsoft.Extensions.DependencyInjection;
// using Silverback.Diagnostics;
// using Silverback.Messaging.Batch;
// using Silverback.Messaging.Broker;
// using Silverback.Messaging.Configuration;
// using Silverback.Messaging.Connectors;
// using Silverback.Messaging.Messages;
// using Silverback.Messaging.Publishing;
// using Silverback.Tests.Integration.TestTypes;
// using Silverback.Tests.Integration.TestTypes.Domain;
// using Xunit;
//
// namespace Silverback.Tests.Integration.Messaging.Connectors
// {
//     public class InboundConnectorTests
//     {
//         private readonly TestSubscriber _testSubscriber;
//
//         private readonly InboundEnvelopeSubscriber _inboundSubscriber;
//
//         private readonly SomeUnhandledMessageSubscriber _someUnhandledMessageSubscriber;
//
//         private readonly IInboundConnector _connector;
//
//         private readonly TestBroker _broker;
//
//         private readonly IErrorPolicyBuilder _errorPolicyBuilder;
//
//         public InboundConnectorTests()
//         {
//             var services = new ServiceCollection();
//
//             _testSubscriber = new TestSubscriber();
//             _inboundSubscriber = new InboundEnvelopeSubscriber();
//             _someUnhandledMessageSubscriber = new SomeUnhandledMessageSubscriber();
//
//             services.AddNullLogger();
//
//             services
//                 .AddSilverback()
//                 .AddSingletonSubscriber(_testSubscriber)
//                 .AddSingletonSubscriber(_inboundSubscriber)
//                 .AddSingletonSubscriber(_someUnhandledMessageSubscriber)
//                 .WithConnectionToMessageBroker(
//                     options => options
//                         .AddBroker<TestBroker>()
//                         .AddInMemoryChunkStore());
//
//             var serviceProvider = services.BuildServiceProvider(
//                 new ServiceProviderOptions
//                 {
//                     ValidateScopes = true
//                 });
//             _broker = (TestBroker)serviceProvider.GetService<IBroker>();
//             _connector = new InboundConnector(
//                 serviceProvider.GetRequiredService<IBrokerCollection>(),
//                 serviceProvider,
//                 serviceProvider.GetRequiredService<ISilverbackLogger<InboundConnector>>());
//             _errorPolicyBuilder = new ErrorPolicyBuilder(serviceProvider);
//         }
//
//         [Fact]
//         public async Task Bind_PushMessages_MessagesReceived()
//         {
//             _connector.Bind(TestConsumerEndpoint.GetDefault());
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestHandleMessage(new TestEventOne());
//             await consumer.TestHandleMessage(new TestEventTwo());
//             await consumer.TestHandleMessage(new TestEventOne());
//             await consumer.TestHandleMessage(new TestEventTwo());
//             await consumer.TestHandleMessage(new TestEventTwo());
//
//             _testSubscriber.ReceivedMessages.Count(message => message is IIntegrationEvent)
//                 .Should().Be(5);
//         }
//
//         [Fact]
//         public async Task Bind_PushUnhandledMessages_MessagesUnwrappedAndReceived()
//         {
//             /* Test for issue #33: messages don't have to be registered with HandleMessagesOfType to be unwrapped
//              * and received */
//
//             _connector.Bind(TestConsumerEndpoint.GetDefault());
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestHandleMessage(new SomeUnhandledMessage { Content = "abc" });
//             await consumer.TestHandleMessage(new SomeUnhandledMessage { Content = "def" });
//             await consumer.TestHandleMessage(new SomeUnhandledMessage { Content = "ghi" });
//
//             _someUnhandledMessageSubscriber.ReceivedMessages.Count.Should().Be(3);
//         }
//
//         [Fact]
//         public async Task Bind_PushMessages_WrappedInboundMessagesReceived()
//         {
//             _connector.Bind(TestConsumerEndpoint.GetDefault());
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestHandleMessage(new TestEventOne());
//             await consumer.TestHandleMessage(new TestEventTwo());
//             await consumer.TestHandleMessage(new TestEventOne());
//             await consumer.TestHandleMessage(new TestEventTwo());
//             await consumer.TestHandleMessage(new TestEventTwo());
//
//             _inboundSubscriber.ReceivedEnvelopes.OfType<InboundEnvelope<TestEventOne>>().Count().Should().Be(2);
//             _inboundSubscriber.ReceivedEnvelopes.OfType<InboundEnvelope<TestEventTwo>>().Count().Should().Be(3);
//         }
//
//         [Fact]
//         public async Task Bind_PushMessages_HeadersReceivedWithInboundMessages()
//         {
//             _connector.Bind(TestConsumerEndpoint.GetDefault());
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestHandleMessage(
//                 new TestEventOne(),
//                 new[] { new MessageHeader("name", "value1") });
//             await consumer.TestHandleMessage(
//                 new TestEventOne(),
//                 new[] { new MessageHeader("name", "value2") });
//
//             var envelopes = _inboundSubscriber.ReceivedEnvelopes.OfType<IInboundEnvelope>().ToList();
//             var firstMessage = envelopes.First();
//             firstMessage.Headers.Count.Should().Be(2);
//             firstMessage.Headers.Select(h => h.Name).Should().BeEquivalentTo("name", "x-message-type");
//             firstMessage.Headers.GetValue("name").Should().Be("value1");
//             var secondMessage = envelopes.Skip(1).First();
//             secondMessage.Headers.Count.Should().Be(2);
//             secondMessage.Headers.Select(h => h.Name).Should().BeEquivalentTo("name", "x-message-type");
//             secondMessage.Headers.GetValue("name").Should().Be("value2");
//         }
//
//         [Fact]
//         public async Task Bind_PushMessages_FailedAttemptsReceivedWithInboundMessages()
//         {
//             _connector.Bind(TestConsumerEndpoint.GetDefault());
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestHandleMessage(new TestEventOne());
//             await consumer.TestHandleMessage(
//                 new TestEventOne(),
//                 new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "3") });
//
//             var inboundMessages = _inboundSubscriber.ReceivedEnvelopes.OfType<IInboundEnvelope>().ToList();
//             inboundMessages.First().Headers.GetValue<int>(DefaultMessageHeaders.FailedAttempts).Should().Be(null);
//             inboundMessages.Skip(1).First().Headers.GetValue<int>(DefaultMessageHeaders.FailedAttempts).Should().Be(3);
//         }
//
//         [Fact]
//         public async Task Bind_PushMessagesInBatch_MessagesReceived()
//         {
//             _connector.Bind(
//                 TestConsumerEndpoint.GetDefault(),
//                 settings: new InboundConnectorSettings
//                 {
//                     Batch = new BatchSettings
//                     {
//                         Size = 5
//                     }
//                 });
//
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestHandleMessage(new TestEventOne());
//             await consumer.TestHandleMessage(new TestEventTwo());
//             await consumer.TestHandleMessage(new TestEventOne());
//             await consumer.TestHandleMessage(new TestEventTwo());
//
//             _testSubscriber.ReceivedMessages.Count(message => message is IIntegrationEvent)
//                 .Should().Be(0);
//
//             await consumer.TestHandleMessage(new TestEventTwo());
//
//             _testSubscriber.ReceivedMessages.Count(message => message is IIntegrationEvent)
//                 .Should().Be(5);
//         }
//
//         [Fact]
//         public async Task Bind_PushMessagesInBatch_BatchEventsPublished()
//         {
//             _connector.Bind(
//                 TestConsumerEndpoint.GetDefault(),
//                 settings: new InboundConnectorSettings
//                 {
//                     Batch = new BatchSettings
//                     {
//                         Size = 5
//                     }
//                 });
//
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestHandleMessage(new TestEventOne());
//
//             _testSubscriber.ReceivedMessages.Count(message => message is BatchStartedEvent)
//                 .Should().Be(1);
//
//             await consumer.TestHandleMessage(new TestEventTwo());
//             await consumer.TestHandleMessage(new TestEventOne());
//             await consumer.TestHandleMessage(new TestEventTwo());
//             await consumer.TestHandleMessage(new TestEventTwo());
//
//             _testSubscriber.ReceivedMessages.Count(message => message is BatchCompleteEvent)
//                 .Should().Be(1);
//             _testSubscriber.ReceivedMessages.Count(message => message is BatchProcessedEvent)
//                 .Should().Be(1);
//             _testSubscriber.ReceivedMessages.Count(message => message is BatchStartedEvent)
//                 .Should().Be(1);
//         }
//
//         [Fact]
//         public async Task Bind_PushMessagesInMultipleBatches_MessagesReceived()
//         {
//             _connector.Bind(
//                 TestConsumerEndpoint.GetDefault(),
//                 settings: new InboundConnectorSettings
//                 {
//                     Batch = new BatchSettings
//                     {
//                         Size = 5
//                     }
//                 });
//
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestHandleMessage(new TestEventOne());
//             await consumer.TestHandleMessage(new TestEventTwo());
//             await consumer.TestHandleMessage(new TestEventOne());
//             await consumer.TestHandleMessage(new TestEventTwo());
//             await consumer.TestHandleMessage(new TestEventTwo());
//             await consumer.TestHandleMessage(new TestEventTwo());
//             await consumer.TestHandleMessage(new TestEventTwo());
//
//             _testSubscriber.ReceivedMessages.Count(message => message is IIntegrationEvent)
//                 .Should().Be(5);
//             _testSubscriber.ReceivedMessages.Clear();
//
//             await consumer.TestHandleMessage(new TestEventOne());
//             await consumer.TestHandleMessage(new TestEventTwo());
//             await consumer.TestHandleMessage(new TestEventOne());
//
//             _testSubscriber.ReceivedMessages.Count(message => message is IIntegrationEvent)
//                 .Should().Be(5);
//         }
//
//         [Fact]
//         public async Task Bind_PushMessagesToMultipleConsumers_MessagesReceived()
//         {
//             _connector.Bind(
//                 TestConsumerEndpoint.GetDefault(),
//                 settings: new InboundConnectorSettings
//                 {
//                     Consumers = 5
//                 });
//
//             _broker.Connect();
//
//             for (int i = 0; i < 3; i++)
//             {
//                 foreach (var consumer in _broker.Consumers.OfType<TestConsumer>())
//                 {
//                     await consumer.TestHandleMessage(new TestEventOne());
//                 }
//             }
//
//             _testSubscriber.ReceivedMessages.Count(message => message is IIntegrationEvent)
//                 .Should().Be(15);
//         }
//
//         [Fact]
//         public async Task Bind_PushMessagesToMultipleConsumersInBatch_MessagesReceived()
//         {
//             _connector.Bind(
//                 TestConsumerEndpoint.GetDefault(),
//                 settings: new InboundConnectorSettings
//                 {
//                     Batch = new BatchSettings
//                     {
//                         Size = 5
//                     },
//                     Consumers = 5
//                 });
//
//             _broker.Connect();
//
//             for (int i = 0; i < 4; i++)
//             {
//                 foreach (var consumer in _broker.Consumers.OfType<TestConsumer>())
//                 {
//                     await consumer.TestHandleMessage(new TestEventOne());
//                 }
//             }
//
//             _testSubscriber.ReceivedMessages.Count(message => message is IIntegrationEvent)
//                 .Should().Be(0);
//
//             foreach (var consumer in _broker.Consumers.OfType<TestConsumer>().Take(3))
//             {
//                 await consumer.TestHandleMessage(new TestEventOne());
//             }
//
//             _testSubscriber.ReceivedMessages.Count(message => message is IIntegrationEvent)
//                 .Should().Be(15);
//
//             _testSubscriber.ReceivedMessages.Clear();
//
//             foreach (var consumer in _broker.Consumers.OfType<TestConsumer>().Skip(3))
//             {
//                 await consumer.TestHandleMessage(new TestEventOne());
//             }
//
//             _testSubscriber.ReceivedMessages.Count(message => message is IIntegrationEvent)
//                 .Should().Be(10);
//         }
//
//         [Fact]
//         public async Task Bind_PushMessageChunks_FullMessageReceived()
//         {
//             _connector.Bind(TestConsumerEndpoint.GetDefault());
//             _broker.Connect();
//
//             var buffer = Convert.FromBase64String(
//                 "eyJDb250ZW50IjoiQSBmdWxsIG1lc3NhZ2UhIiwiSWQiOiI0Mjc1ODMwMi1kOGU5LTQzZjktYjQ3ZS1kN2FjNDFmMmJiMDMifQ==");
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestConsume(
//                 buffer.Take(40).ToArray(),
//                 HeadersHelper.GetChunkHeaders<TestEventOne>("123", 1, 4));
//             await consumer.TestConsume(
//                 buffer.Skip(40).Take(40).ToArray(),
//                 HeadersHelper.GetChunkHeaders<TestEventOne>("123", 2, 4));
//             await consumer.TestConsume(
//                 buffer.Skip(80).Take(40).ToArray(),
//                 HeadersHelper.GetChunkHeaders<TestEventOne>("123", 3, 4));
//             await consumer.TestConsume(
//                 buffer.Skip(120).ToArray(),
//                 HeadersHelper.GetChunkHeaders<TestEventOne>("123", 4, 4));
//
//             _testSubscriber.ReceivedMessages.Count(message => message is IIntegrationEvent)
//                 .Should().Be(1);
//             _testSubscriber.ReceivedMessages.First(message => message is IIntegrationEvent).As<TestEventOne>()
//                 .Content.Should().Be("A full message!");
//         }
//
//         [Fact]
//         public async Task Bind_PushMessageChunksInRandomOrder_FullMessageReceived()
//         {
//             _connector.Bind(TestConsumerEndpoint.GetDefault());
//             _broker.Connect();
//
//             var buffer = Convert.FromBase64String(
//                 "eyJDb250ZW50IjoiQSBmdWxsIG1lc3NhZ2UhIiwiSWQiOiI0Mjc1ODMwMi1kOGU5LTQzZjktYjQ3ZS1kN2FjNDFmMmJiMDMifQ==");
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestConsume(
//                 buffer.Take(40).ToArray(),
//                 HeadersHelper.GetChunkHeaders<TestEventOne>("123", 1, 4));
//             await consumer.TestConsume(
//                 buffer.Skip(120).ToArray(),
//                 HeadersHelper.GetChunkHeaders<TestEventOne>("123", 4, 4));
//             await consumer.TestConsume(
//                 buffer.Skip(80).Take(40).ToArray(),
//                 HeadersHelper.GetChunkHeaders<TestEventOne>("123", 3, 4));
//             await consumer.TestConsume(
//                 buffer.Skip(40).Take(40).ToArray(),
//                 HeadersHelper.GetChunkHeaders<TestEventOne>("123", 2, 4));
//
//             _testSubscriber.ReceivedMessages.Count(message => message is IIntegrationEvent)
//                 .Should().Be(1);
//             _testSubscriber.ReceivedMessages.First(message => message is IIntegrationEvent).As<TestEventOne>()
//                 .Content.Should().Be("A full message!");
//         }
//
//         [Fact]
//         public async Task Bind_PushMessageChunksWithDuplicates_FullMessageReceived()
//         {
//             _connector.Bind(TestConsumerEndpoint.GetDefault());
//             _broker.Connect();
//
//             var buffer = Convert.FromBase64String(
//                 "eyJDb250ZW50IjoiQSBmdWxsIG1lc3NhZ2UhIiwiSWQiOiI0Mjc1ODMwMi1kOGU5LTQzZjktYjQ3ZS1kN2FjNDFmMmJiMDMifQ==");
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestConsume(
//                 buffer.Take(40).ToArray(),
//                 HeadersHelper.GetChunkHeaders<TestEventOne>("123", 1, 4));
//             await consumer.TestConsume(
//                 buffer.Skip(120).ToArray(),
//                 HeadersHelper.GetChunkHeaders<TestEventOne>("123", 4, 4));
//             await consumer.TestConsume(
//                 buffer.Skip(80).Take(40).ToArray(),
//                 HeadersHelper.GetChunkHeaders<TestEventOne>("123", 3, 4));
//             await consumer.TestConsume(
//                 buffer.Skip(120).ToArray(),
//                 HeadersHelper.GetChunkHeaders<TestEventOne>("123", 4, 4));
//             await consumer.TestConsume(
//                 buffer.Skip(80).Take(40).ToArray(),
//                 HeadersHelper.GetChunkHeaders<TestEventOne>("123", 3, 4));
//             await consumer.TestConsume(
//                 buffer.Skip(40).Take(40).ToArray(),
//                 HeadersHelper.GetChunkHeaders<TestEventOne>("123", 2, 4));
//
//             _testSubscriber.ReceivedMessages.Count(message => message is IIntegrationEvent)
//                 .Should().Be(1);
//             _testSubscriber.ReceivedMessages.First(message => message is IIntegrationEvent).As<TestEventOne>()
//                 .Content.Should().Be("A full message!");
//         }
//
//         [Fact]
//         public async Task Bind_PushMessages_Acknowledged()
//         {
//             _connector.Bind(TestConsumerEndpoint.GetDefault());
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestHandleMessage(new TestEventOne(), offset: new TestOffset());
//             await consumer.TestHandleMessage(new TestEventTwo(), offset: new TestOffset());
//             await consumer.TestHandleMessage(new TestEventOne(), offset: new TestOffset());
//             await consumer.TestHandleMessage(new TestEventTwo(), offset: new TestOffset());
//             await consumer.TestHandleMessage(new TestEventTwo(), offset: new TestOffset());
//
//             consumer.AcknowledgeCount.Should().Be(5);
//         }
//
//         [Fact]
//         public async Task Bind_PushMessagesInBatch_Acknowledged()
//         {
//             _connector.Bind(
//                 TestConsumerEndpoint.GetDefault(),
//                 settings: new InboundConnectorSettings
//                 {
//                     Batch = new BatchSettings
//                     {
//                         Size = 5
//                     }
//                 });
//
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestHandleMessage(new TestEventOne(), offset: new TestOffset());
//             await consumer.TestHandleMessage(new TestEventTwo(), offset: new TestOffset());
//             await consumer.TestHandleMessage(new TestEventOne(), offset: new TestOffset());
//             await consumer.TestHandleMessage(new TestEventTwo(), offset: new TestOffset());
//
//             consumer.AcknowledgeCount.Should().Be(0);
//
//             await consumer.TestHandleMessage(new TestEventTwo(), offset: new TestOffset());
//
//             consumer.AcknowledgeCount.Should().Be(5);
//         }
//
//         [Fact]
//         public async Task Bind_PushMessagesInMultipleBatches_Acknowledged()
//         {
//             _connector.Bind(
//                 TestConsumerEndpoint.GetDefault(),
//                 settings: new InboundConnectorSettings
//                 {
//                     Batch = new BatchSettings
//                     {
//                         Size = 5
//                     }
//                 });
//
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//
//             await consumer.TestHandleMessage(new TestEventTwo(), offset: new TestOffset());
//             await consumer.TestHandleMessage(new TestEventOne(), offset: new TestOffset());
//             await consumer.TestHandleMessage(new TestEventOne(), offset: new TestOffset());
//             await consumer.TestHandleMessage(new TestEventTwo(), offset: new TestOffset());
//             await consumer.TestHandleMessage(new TestEventTwo(), offset: new TestOffset());
//             await consumer.TestHandleMessage(new TestEventTwo(), offset: new TestOffset());
//             await consumer.TestHandleMessage(new TestEventTwo(), offset: new TestOffset());
//
//             consumer.AcknowledgeCount.Should().Be(5);
//
//             await consumer.TestHandleMessage(new TestEventOne(), offset: new TestOffset());
//             await consumer.TestHandleMessage(new TestEventTwo(), offset: new TestOffset());
//             await consumer.TestHandleMessage(new TestEventOne(), offset: new TestOffset());
//
//             consumer.AcknowledgeCount.Should().Be(10);
//         }
//
//         [Fact]
//         public async Task Bind_PushMessagesToMultipleConsumers_Acknowledged()
//         {
//             _connector.Bind(
//                 TestConsumerEndpoint.GetDefault(),
//                 settings: new InboundConnectorSettings
//                 {
//                     Consumers = 5
//                 });
//
//             _broker.Connect();
//
//             for (int i = 0; i < 3; i++)
//             {
//                 foreach (var consumer in _broker.Consumers.OfType<TestConsumer>())
//                 {
//                     await consumer.TestHandleMessage(new TestEventOne(), offset: new TestOffset());
//                 }
//             }
//
//             foreach (var consumer in _broker.Consumers.OfType<TestConsumer>())
//             {
//                 consumer.AcknowledgeCount.Should().Be(3);
//             }
//         }
//
//         [Fact]
//         public async Task Bind_PushMessagesToMultipleConsumersInBatch_Acknowledged()
//         {
//             _connector.Bind(
//                 TestConsumerEndpoint.GetDefault(),
//                 settings: new InboundConnectorSettings
//                 {
//                     Batch = new BatchSettings
//                     {
//                         Size = 5
//                     },
//                     Consumers = 5
//                 });
//
//             _broker.Connect();
//
//             for (int i = 0; i < 4; i++)
//             {
//                 foreach (var consumer in _broker.Consumers.OfType<TestConsumer>())
//                 {
//                     await consumer.TestHandleMessage(new TestEventOne(), offset: new TestOffset());
//                 }
//             }
//
//             _broker.Consumers.OfType<TestConsumer>().Sum(c => c.AcknowledgeCount).Should().Be(0);
//
//             foreach (var consumer in _broker.Consumers.OfType<TestConsumer>().Take(3))
//             {
//                 await consumer.TestHandleMessage(new TestEventOne(), offset: new TestOffset());
//             }
//
//             _broker.Consumers.OfType<TestConsumer>().Sum(c => c.AcknowledgeCount).Should().Be(15);
//
//             foreach (var consumer in _broker.Consumers.OfType<TestConsumer>().Skip(3))
//             {
//                 await consumer.TestHandleMessage(new TestEventOne(), offset: new TestOffset());
//             }
//
//             _broker.Consumers.OfType<TestConsumer>().Sum(c => c.AcknowledgeCount).Should().Be(25);
//         }
//
//         [Fact]
//         public async Task Bind_WithRetryErrorPolicy_RetriedAndReceived()
//         {
//             _testSubscriber.MustFailCount = 3;
//             _connector.Bind(TestConsumerEndpoint.GetDefault(), _errorPolicyBuilder.Retry().MaxFailedAttempts(3));
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestHandleMessage(new TestEventOne { Content = "Test" });
//
//             _testSubscriber.FailCount.Should().Be(3);
//             _testSubscriber.ReceivedMessages.Count(message => message is IIntegrationEvent)
//                 .Should().Be(4);
//         }
//
//         [Fact]
//         public async Task Bind_WithRetryErrorPolicyToHandleDeserializerErrors_RetriedAndReceived()
//         {
//             var testSerializer = new TestSerializer { MustFailCount = 3 };
//             _connector.Bind(
//                 new TestConsumerEndpoint("test")
//                 {
//                     Serializer = testSerializer
//                 },
//                 _errorPolicyBuilder.Retry().MaxFailedAttempts(3));
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestHandleMessage(new TestEventOne { Content = "Test" });
//
//             testSerializer.FailCount.Should().Be(3);
//             _testSubscriber.ReceivedMessages.Count(message => message is IIntegrationEvent).Should().Be(1);
//         }
//
//         [Fact]
//         public async Task Bind_WithRetryErrorPolicyToHandleDeserializerErrorsInChunkedMessage_RetriedAndReceived()
//         {
//             var testSerializer = new TestSerializer { MustFailCount = 3 };
//             _connector.Bind(
//                 new TestConsumerEndpoint("test")
//                 {
//                     Serializer = testSerializer
//                 },
//                 _errorPolicyBuilder.Retry().MaxFailedAttempts(3));
//             _broker.Connect();
//
//             var buffer = Convert.FromBase64String(
//                 "eyJDb250ZW50IjoiQSBmdWxsIG1lc3NhZ2UhIiwiSWQiOiI0Mjc1ODMwMi1kOGU5LTQzZjktYjQ3ZS1kN2FjNDFmMmJiMDMifQ==");
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestConsume(
//                 buffer.Take(20).ToArray(),
//                 HeadersHelper.GetChunkHeaders<TestEventOne>("123", 1, 4));
//             await consumer.TestConsume(
//                 buffer.Skip(20).Take(20).ToArray(),
//                 HeadersHelper.GetChunkHeaders<TestEventOne>("123", 2, 4));
//             await consumer.TestConsume(
//                 buffer.Skip(40).Take(20).ToArray(),
//                 HeadersHelper.GetChunkHeaders<TestEventOne>("123", 3, 4));
//             await consumer.TestConsume(
//                 buffer.Skip(60).ToArray(),
//                 HeadersHelper.GetChunkHeaders<TestEventOne>("123", 4, 4));
//
//             testSerializer.FailCount.Should().Be(3);
//             _testSubscriber.ReceivedMessages.Count(message => message is IIntegrationEvent)
//                 .Should().Be(1);
//             _testSubscriber.ReceivedMessages.OfType<TestEventOne>().First().Content.Should().Be("A full message!");
//         }
//
//         [Fact]
//         public async Task Bind_WithChainedErrorPolicy_RetriedAndMoved()
//         {
//             _testSubscriber.MustFailCount = 3;
//             _connector.Bind(
//                 TestConsumerEndpoint.GetDefault(),
//                 _errorPolicyBuilder.Chain(
//                     _errorPolicyBuilder.Retry().MaxFailedAttempts(1),
//                     _errorPolicyBuilder.Move(new TestProducerEndpoint("bad"))));
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestHandleMessage(new TestEventOne { Content = "Test" });
//
//             var producer = (TestProducer)_broker.GetProducer(new TestProducerEndpoint("bad"));
//
//             _testSubscriber.FailCount.Should().Be(2);
//             _testSubscriber.ReceivedMessages.Count(message => message is IIntegrationEvent)
//                 .Should().Be(2);
//             producer.ProducedMessages.Count.Should().Be(1);
//         }
//
//         [Fact]
//         public async Task Bind_WithChainedErrorPolicy_OneAndOnlyOneFailedAttemptsHeaderIsAdded()
//         {
//             _testSubscriber.MustFailCount = 3;
//             _connector.Bind(
//                 TestConsumerEndpoint.GetDefault(),
//                 _errorPolicyBuilder.Chain(
//                     _errorPolicyBuilder.Retry().MaxFailedAttempts(1),
//                     _errorPolicyBuilder.Move(new TestProducerEndpoint("bad"))));
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestHandleMessage(new TestEventOne { Content = "Test" });
//
//             var producer = (TestProducer)_broker.GetProducer(new TestProducerEndpoint("bad"));
//
//             producer.ProducedMessages.Last().Headers.Count(h => h.Name == DefaultMessageHeaders.FailedAttempts).Should()
//                 .Be(1);
//         }
//
//         [Fact]
//         public async Task Bind_WithRetryErrorPolicy_RetriedAndReceivedInBatch()
//         {
//             _testSubscriber.MustFailCount = 3;
//             _connector.Bind(
//                 TestConsumerEndpoint.GetDefault(),
//                 _errorPolicyBuilder.Retry().MaxFailedAttempts(3),
//                 new InboundConnectorSettings
//                 {
//                     Batch = new BatchSettings
//                     {
//                         Size = 2
//                     }
//                 });
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestHandleMessage(new TestEventOne { Content = "Test" });
//             await consumer.TestHandleMessage(new TestEventOne { Content = "Test" });
//
//             _testSubscriber.FailCount.Should().Be(3);
//             _testSubscriber.ReceivedMessages.OfType<TestEventOne>().Count().Should().Be(5);
//         }
//
//         [Fact]
//         public async Task Bind_WithRetryErrorPolicy_BatchEventsCorrectlyPublished()
//         {
//             _testSubscriber.MustFailCount = 3;
//             _connector.Bind(
//                 TestConsumerEndpoint.GetDefault(),
//                 _errorPolicyBuilder.Retry().MaxFailedAttempts(3),
//                 new InboundConnectorSettings
//                 {
//                     Batch = new BatchSettings
//                     {
//                         Size = 2
//                     }
//                 });
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestHandleMessage(new TestEventOne { Content = "Test" });
//             await consumer.TestHandleMessage(new TestEventOne { Content = "Test" });
//
//             _testSubscriber.FailCount.Should().Be(3);
//             _testSubscriber.ReceivedMessages.OfType<BatchAbortedEvent>().Count().Should().Be(3);
//             _testSubscriber.ReceivedMessages.OfType<BatchCompleteEvent>().Count().Should().Be(4);
//             _testSubscriber.ReceivedMessages.OfType<BatchProcessedEvent>().Count().Should().Be(1);
//         }
//
//         [Fact]
//         public async Task Bind_WithRetryErrorPolicyToHandleDeserializerErrors_RetriedAndReceivedInBatch()
//         {
//             var testSerializer = new TestSerializer { MustFailCount = 3 };
//             _connector.Bind(
//                 new TestConsumerEndpoint("test")
//                 {
//                     Serializer = testSerializer
//                 },
//                 _errorPolicyBuilder.Retry().MaxFailedAttempts(3),
//                 new InboundConnectorSettings
//                 {
//                     Batch = new BatchSettings
//                     {
//                         Size = 2
//                     }
//                 });
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestHandleMessage(new TestEventOne { Content = "Test" });
//             await consumer.TestHandleMessage(new TestEventOne { Content = "Test" });
//
//             testSerializer.FailCount.Should().Be(3);
//             _testSubscriber.ReceivedMessages.OfType<BatchCompleteEvent>().Count().Should().Be(4);
//             _testSubscriber.ReceivedMessages.OfType<BatchAbortedEvent>().Count().Should().Be(3);
//             _testSubscriber.ReceivedMessages.OfType<BatchProcessedEvent>().Count().Should().Be(1);
//             _testSubscriber.ReceivedMessages.OfType<TestEventOne>().Count().Should().Be(2);
//         }
//
//         [Fact]
//         public void Bind_PushUnsubscribedMessage_NoExceptionThrown()
//         {
//             _connector.Bind(TestConsumerEndpoint.GetDefault());
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             Func<Task> act = () => consumer.TestHandleMessage(new UnsubscribedMessage());
//
//             act.Should().NotThrow();
//         }
//
//         [Fact]
//         public void Bind_PushUnsubscribedMessageWithThrowIfUnhandled_ExceptionThrown()
//         {
//             _connector.Bind(TestConsumerEndpoint.GetDefault());
//             _connector.Bind(
//                 new TestConsumerEndpoint("test")
//                 {
//                     ThrowIfUnhandled = true
//                 });
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             Func<Task> act = () => consumer.TestHandleMessage(new UnsubscribedMessage());
//
//             act.Should().ThrowExactly<UnhandledMessageException>();
//         }
//     }
// }
