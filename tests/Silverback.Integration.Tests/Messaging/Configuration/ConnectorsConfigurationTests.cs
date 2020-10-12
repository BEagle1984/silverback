// TODO: Migrate or delete tests

// // Copyright (c) 2020 Sergio Aquilini
// // This code is licensed under MIT license (see LICENSE file for details)
//
// using System;
// using System.Linq;
// using System.Threading.Tasks;
// using FluentAssertions;
// using Microsoft.Extensions.DependencyInjection;
// using Silverback.Messaging.Configuration;
// using Silverback.Messaging.Connectors.Repositories;
// using Silverback.Messaging.Messages;
// using Silverback.Messaging.Publishing;
// using Silverback.Tests.Integration.TestTypes;
// using Silverback.Tests.Integration.TestTypes.Domain;
// using Xunit;
//
// namespace Silverback.Tests.Integration.Messaging.Configuration
// {
//     public class ConnectorsConfigurationTests
//     {
//         private readonly TestSubscriber _testSubscriber = new TestSubscriber();
//
//         [Fact]
//         public void AddOutboundConnector_PublishMessages_MessagesProduced()
//         {
//             var serviceProvider = ServiceProviderHelper.GetServiceProvider(
//                 services => services
//                     .AddSilverback()
//                     .WithConnectionToMessageBroker(
//                         options => options
//                             .AddBroker<TestBroker>()
//                             .AddOutboundConnector())
//                     .AddSingletonSubscriber(_testSubscriber)
//                     .AddEndpoints(
//                         endpoints => endpoints
//                             .AddOutbound<IIntegrationMessage>(TestProducerEndpoint.GetDefault())));
//
//             var broker = serviceProvider.GetRequiredService<TestBroker>();
//             broker.Connect();
//
//             var publisher = serviceProvider.GetRequiredService<IPublisher>();
//             publisher.Publish(new TestEventOne());
//             publisher.Publish(new TestEventTwo());
//             publisher.Publish(new TestEventOne());
//             publisher.Publish(new TestEventTwo());
//             publisher.Publish(new TestEventTwo());
//
//             broker.ProducedMessages.Should().HaveCount(5);
//         }
//
//         [Fact]
//         public async Task AddDeferredOutboundConnector_PublishMessages_MessagesQueued()
//         {
//             var serviceProvider = ServiceProviderHelper.GetServiceProvider(
//                 services => services
//                     .AddSilverback()
//                     .WithConnectionToMessageBroker(
//                         options => options
//                             .AddBroker<TestBroker>()
//                             .AddDeferredOutboundConnector<InMemoryOutboundQueue>())
//                     .AddSingletonSubscriber(_testSubscriber)
//                     .AddEndpoints(
//                         endpoints => endpoints
//                             .AddOutbound<IIntegrationMessage>(TestProducerEndpoint.GetDefault())));
//             var broker = serviceProvider.GetRequiredService<TestBroker>();
//             broker.Connect();
//
//             var publisher = serviceProvider.GetRequiredService<IPublisher>();
//             await publisher.PublishAsync(new TestEventOne());
//             await publisher.PublishAsync(new TestEventTwo());
//             await publisher.PublishAsync(new TestEventOne());
//             await publisher.PublishAsync(new TestEventTwo());
//             await publisher.PublishAsync(new TestEventTwo());
//             await publisher.PublishAsync(new TransactionCompletedEvent());
//
//             var outboundQueue = (InMemoryOutboundQueue)serviceProvider.GetRequiredService<IOutboundQueueWriter>();
//             (await outboundQueue.GetLength()).Should().Be(5);
//         }
//
//         [Fact]
//         public async Task AddDeferredOutboundConnector_Rollback_MessagesNotQueued()
//         {
//             var serviceProvider = ServiceProviderHelper.GetServiceProvider(
//                 services => services
//                     .AddSilverback()
//                     .WithConnectionToMessageBroker(
//                         options => options
//                             .AddBroker<TestBroker>()
//                             .AddDeferredOutboundConnector<InMemoryOutboundQueue>())
//                     .AddSingletonSubscriber(_testSubscriber)
//                     .AddEndpoints(
//                         endpoints => endpoints
//                             .AddOutbound<IIntegrationMessage>(TestProducerEndpoint.GetDefault())));
//
//             var broker = serviceProvider.GetRequiredService<TestBroker>();
//             broker.Connect();
//
//             var publisher = serviceProvider.GetRequiredService<IPublisher>();
//             await publisher.PublishAsync(new TestEventOne());
//             await publisher.PublishAsync(new TestEventTwo());
//             await publisher.PublishAsync(new TransactionCompletedEvent());
//             await publisher.PublishAsync(new TestEventOne());
//             await publisher.PublishAsync(new TestEventTwo());
//             await publisher.PublishAsync(new TestEventTwo());
//             await publisher.PublishAsync(new TransactionAbortedEvent());
//
//             var outboundQueue = (InMemoryOutboundQueue)serviceProvider.GetRequiredService<IOutboundQueueWriter>();
//             (await outboundQueue.GetLength()).Should().Be(2);
//         }
//
//         [Fact]
//         public async Task AddInboundConnector_PushMessages_MessagesReceived()
//         {
//             var serviceProvider = ServiceProviderHelper.GetServiceProvider(
//                 services => services
//                     .AddSilverback()
//                     .WithConnectionToMessageBroker(
//                         options => options
//                             .AddBroker<TestBroker>()
//                             .AddInboundConnector())
//                     .AddSingletonSubscriber(_testSubscriber)
//                     .AddEndpoints(
//                         endpoints => endpoints
//                             .AddInbound(TestConsumerEndpoint.GetDefault())));
//
//             var broker = serviceProvider.GetRequiredService<TestBroker>();
//             broker.Connect();
//
//             var consumer = (TestConsumer)broker.Consumers[0];
//             await consumer.TestHandleMessage(new TestEventOne());
//             await consumer.TestHandleMessage(new TestEventTwo());
//             await consumer.TestHandleMessage(new TestEventOne());
//             await consumer.TestHandleMessage(new TestEventTwo());
//             await consumer.TestHandleMessage(new TestEventTwo());
//
//             _testSubscriber.ReceivedMessages
//                 .Count(message => !(message is ISilverbackEvent))
//                 .Should().Be(5);
//         }
//
//         [Fact]
//         public async Task AddInboundConnector_CalledMultipleTimes_EachMessageReceivedOnce()
//         {
//             var serviceProvider = ServiceProviderHelper.GetServiceProvider(
//                 services => services
//                     .AddSilverback()
//                     .WithConnectionToMessageBroker(
//                         options => options
//                             .AddBroker<TestBroker>()
//                             .AddInboundConnector()
//                             .AddInboundConnector())
//                     .AddSingletonSubscriber(_testSubscriber)
//                     .AddEndpoints(
//                         endpoints => endpoints
//                             .AddInbound(TestConsumerEndpoint.GetDefault())));
//
//             var broker = serviceProvider.GetRequiredService<TestBroker>();
//             broker.Connect();
//
//             var consumer = (TestConsumer)broker.Consumers[0];
//             await consumer.TestHandleMessage(new TestEventOne());
//             await consumer.TestHandleMessage(new TestEventTwo());
//             await consumer.TestHandleMessage(new TestEventOne());
//             await consumer.TestHandleMessage(new TestEventTwo());
//             await consumer.TestHandleMessage(new TestEventTwo());
//
//             _testSubscriber.ReceivedMessages
//                 .Count(message => !(message is ISilverbackEvent))
//                 .Should().Be(5);
//         }
//
//         [Fact]
//         public async Task AddLoggedInboundConnector_PushMessages_MessagesReceived()
//         {
//             var serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
//                 .AddScoped<IInboundLog, InMemoryInboundLog>()
//                 .AddSilverback().WithConnectionToMessageBroker(
//                     options => options
//                         .AddBroker<TestBroker>()
//                         .AddLoggedInboundConnector())
//                 .AddSingletonSubscriber(_testSubscriber)
//                 .AddEndpoints(
//                     endpoints =>
//                         endpoints
//                             .AddInbound(TestConsumerEndpoint.GetDefault())));
//
//             var broker = serviceProvider.GetRequiredService<TestBroker>();
//             broker.Connect();
//
//             var consumer = (TestConsumer)broker.Consumers[0];
//             var duplicatedId = Guid.NewGuid();
//             await consumer.TestHandleMessage(
//                 new TestEventOne(),
//                 GetMessageIdHeader(Guid.NewGuid()));
//             await consumer.TestHandleMessage(
//                 new TestEventOne(),
//                 GetMessageIdHeader(duplicatedId));
//             await consumer.TestHandleMessage(
//                 new TestEventOne(),
//                 GetMessageIdHeader(Guid.NewGuid()));
//             await consumer.TestHandleMessage(
//                 new TestEventOne(),
//                 GetMessageIdHeader(Guid.NewGuid()));
//             await consumer.TestHandleMessage(
//                 new TestEventOne(),
//                 GetMessageIdHeader(duplicatedId));
//
//             _testSubscriber.ReceivedMessages
//                 .Count(message => !(message is ISilverbackEvent))
//                 .Should().Be(4);
//         }
//
//         [Fact]
//         public async Task AddOffsetStoredInboundConnector_PushMessages_MessagesReceived()
//         {
//             var serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
//                 .AddScoped<IOffsetStore, InMemoryOffsetStore>()
//                 .AddSilverback().WithConnectionToMessageBroker(
//                     options => options
//                         .AddBroker<TestBroker>()
//                         .AddOffsetStoredInboundConnector())
//                 .AddSingletonSubscriber(_testSubscriber)
//                 .AddEndpoints(
//                     endpoints =>
//                         endpoints
//                             .AddInbound(TestConsumerEndpoint.GetDefault())));
//
//             var broker = serviceProvider.GetRequiredService<TestBroker>();
//             broker.Connect();
//
//             var consumer = (TestConsumer)broker.Consumers[0];
//             await consumer.TestHandleMessage(
//                 new TestEventOne(),
//                 offset: new TestOffset("test-1", "1"));
//             await consumer.TestHandleMessage(
//                 new TestEventTwo(),
//                 offset: new TestOffset("test-2", "1"));
//             await consumer.TestHandleMessage(
//                 new TestEventOne(),
//                 offset: new TestOffset("test-1", "2"));
//             await consumer.TestHandleMessage(
//                 new TestEventTwo(),
//                 offset: new TestOffset("test-2", "1"));
//             await consumer.TestHandleMessage(
//                 new TestEventOne(),
//                 offset: new TestOffset("test-1", "3"));
//             await consumer.TestHandleMessage(
//                 new TestEventTwo(),
//                 offset: new TestOffset("test-2", "2"));
//
//             _testSubscriber.ReceivedMessages
//                 .Count(message => !(message is ISilverbackEvent))
//                 .Should().Be(5);
//         }
//
//         private static MessageHeader[] GetMessageIdHeader(Guid id) =>
//             new[]
//             {
//                 new MessageHeader("x-message-id", id.ToString())
//             };
//     }
// }
