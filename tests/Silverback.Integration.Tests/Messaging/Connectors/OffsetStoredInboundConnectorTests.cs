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
// using Silverback.Messaging.Connectors;
// using Silverback.Messaging.Connectors.Repositories;
// using Silverback.Tests.Integration.TestTypes;
// using Silverback.Tests.Integration.TestTypes.Domain;
// using Xunit;
//
// namespace Silverback.Tests.Integration.Messaging.Connectors
// {
//     public class OffsetStoredInboundConnectorTests
//     {
//         private readonly TestSubscriber _testSubscriber;
//
//         private readonly IInboundConnector _connector;
//
//         private readonly TestBroker _broker;
//
//         private readonly IServiceProvider _scopedServiceProvider;
//
//         public OffsetStoredInboundConnectorTests()
//         {
//             var services = new ServiceCollection();
//
//             _testSubscriber = new TestSubscriber();
//
//             services.AddNullLogger();
//             services
//                 .AddSilverback()
//                 .WithConnectionToMessageBroker(
//                     options => options
//                         .AddBroker<TestBroker>())
//                 .AddSingletonSubscriber(_testSubscriber);
//
//             services.AddScoped<IOffsetStore, InMemoryOffsetStore>();
//
//             IServiceProvider serviceProvider =
//                 services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
//             _broker = (TestBroker)serviceProvider.GetService<IBroker>();
//             _connector = new OffsetStoredInboundConnector(
//                 serviceProvider.GetRequiredService<IBrokerCollection>(),
//                 serviceProvider,
//                 serviceProvider.GetRequiredService<ISilverbackIntegrationLogger<OffsetStoredInboundConnector>>());
//             _scopedServiceProvider = serviceProvider.CreateScope().ServiceProvider;
//         }
//
//         [Fact]
//         public async Task Bind_PushMessages_MessagesReceived()
//         {
//             _connector.Bind(TestConsumerEndpoint.GetDefault());
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestHandleMessage(
//                 new TestEventOne(),
//                 offset: new TestOffset("a", "1"));
//             await consumer.TestHandleMessage(
//                 new TestEventTwo(),
//                 offset: new TestOffset("a", "2"));
//
//             _testSubscriber.ReceivedMessages.Count(message => message is IIntegrationEvent)
//                 .Should().Be(2);
//         }
//
//         [Fact]
//         public async Task Bind_PushMessages_EachIsConsumedOnce()
//         {
//             var e1 = new TestEventOne { Content = "Test" };
//             var e2 = new TestEventTwo { Content = "Test" };
//             var o1 = new TestOffset("a", "1");
//             var o2 = new TestOffset("a", "2");
//
//             _connector.Bind(TestConsumerEndpoint.GetDefault());
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestHandleMessage(e1, offset: o1);
//             await consumer.TestHandleMessage(e2, offset: o2);
//             await consumer.TestHandleMessage(e1, offset: o1);
//             await consumer.TestHandleMessage(e2, offset: o2);
//             await consumer.TestHandleMessage(e1, offset: o1);
//
//             _testSubscriber.ReceivedMessages.Count(message => message is IIntegrationEvent)
//                 .Should().Be(2);
//         }
//
//         [Fact]
//         public async Task Bind_PushMessagesFromDifferentTopics_EachIsConsumedOnce()
//         {
//             var e = new TestEventOne { Content = "Test" };
//             var o1 = new TestOffset("a", "1");
//             var o2 = new TestOffset("b", "1");
//
//             _connector.Bind(TestConsumerEndpoint.GetDefault());
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestHandleMessage(e, offset: o1);
//             await consumer.TestHandleMessage(e, offset: o2);
//             await consumer.TestHandleMessage(e, offset: o1);
//             await consumer.TestHandleMessage(e, offset: o2);
//             await consumer.TestHandleMessage(e, offset: o1);
//
//             _testSubscriber.ReceivedMessages.Count(message => message is IIntegrationEvent)
//                 .Should().Be(2);
//         }
//
//         [Fact]
//         public async Task Bind_PushMessages_OffsetStored()
//         {
//             var e = new TestEventOne { Content = "Test" };
//             var o1 = new TestOffset("a", "1");
//             var o2 = new TestOffset("b", "1");
//             var o3 = new TestOffset("a", "2");
//
//             _connector.Bind(TestConsumerEndpoint.GetDefault());
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestHandleMessage(e, offset: o1);
//             await consumer.TestHandleMessage(e, offset: o2);
//             await consumer.TestHandleMessage(e, offset: o3);
//             await consumer.TestHandleMessage(e, offset: o2);
//             await consumer.TestHandleMessage(e, offset: o1);
//
//             _scopedServiceProvider.GetRequiredService<IOffsetStore>().As<InMemoryOffsetStore>()
//                 .CommittedItemsCount.Should().Be(2);
//         }
//
//         [Fact]
//         public async Task Bind_PushMessagesInBatch_EachIsConsumedOnce()
//         {
//             var e = new TestEventOne { Content = "Test" };
//             var o1 = new TestOffset("a", "1");
//             var o2 = new TestOffset("b", "1");
//             var o3 = new TestOffset("a", "2");
//
//             _connector.Bind(
//                 TestConsumerEndpoint.GetDefault(),
//                 settings: new InboundConnectorSettings
//                 {
//                     Batch = new BatchSettings
//                     {
//                         Size = 2
//                     }
//                 });
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestHandleMessage(e, offset: o1);
//             await consumer.TestHandleMessage(e, offset: o2);
//             await consumer.TestHandleMessage(e, offset: o3);
//             await consumer.TestHandleMessage(e, offset: o2);
//             await consumer.TestHandleMessage(e, offset: o1);
//             await consumer.TestHandleMessage(e, offset: o3);
//
//             _testSubscriber.ReceivedMessages.OfType<TestEventOne>().Should().HaveCount(3);
//         }
//
//         [Fact]
//         public async Task Bind_PushMessagesInBatch_OffsetStored()
//         {
//             var e = new TestEventOne { Content = "Test" };
//             var o1 = new TestOffset("a", "1");
//             var o2 = new TestOffset("b", "1");
//             var o3 = new TestOffset("a", "2");
//
//             _connector.Bind(
//                 TestConsumerEndpoint.GetDefault(),
//                 settings: new InboundConnectorSettings
//                 {
//                     Batch = new BatchSettings
//                     {
//                         Size = 2
//                     }
//                 });
//             _broker.Connect();
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//             await consumer.TestHandleMessage(e, offset: o1);
//             await consumer.TestHandleMessage(e, offset: o2);
//             await consumer.TestHandleMessage(e, offset: o3);
//             await consumer.TestHandleMessage(e, offset: o2);
//             await consumer.TestHandleMessage(e, offset: o1);
//
//             _scopedServiceProvider.GetRequiredService<IOffsetStore>().As<InMemoryOffsetStore>()
//                 .CommittedItemsCount.Should().Be(2);
//         }
//
//         [Fact]
//         public async Task Bind_PushMessagesInBatch_OnlyOffsetOfCommittedBatchStored()
//         {
//             var e = new TestEventOne { Content = "Test" };
//             var fail = new TestEventOne { Content = "FAIL" };
//             var o1 = new TestOffset("a", "1");
//             var o2 = new TestOffset("a", "2");
//             var o3 = new TestOffset("a", "3");
//             var o4 = new TestOffset("a", "4");
//
//             _connector.Bind(
//                 TestConsumerEndpoint.GetDefault(),
//                 settings: new InboundConnectorSettings
//                 {
//                     Batch = new BatchSettings
//                     {
//                         Size = 2
//                     }
//                 });
//             _broker.Connect();
//
//             _testSubscriber.FailCondition = m => m is TestEventOne m2 && m2.Content == "FAIL";
//
//             var consumer = (TestConsumer)_broker.Consumers[0];
//
//             try
//             {
//                 await consumer.TestHandleMessage(e, offset: o1);
//             }
//             catch
//             {
//                 // ignored
//             }
//
//             try
//             {
//                 await consumer.TestHandleMessage(e, offset: o2);
//             }
//             catch
//             {
//                 // ignored
//             }
//
//             try
//             {
//                 await consumer.TestHandleMessage(e, offset: o3);
//             }
//             catch
//             {
//                 // ignored
//             }
//
//             try
//             {
//                 await consumer.TestHandleMessage(fail, offset: o4);
//             }
//             catch
//             {
//                 // ignored
//             }
//
//             IComparableOffset? latestValue = await _scopedServiceProvider.GetRequiredService<IOffsetStore>()
//                 .GetLatestValue("a", consumer.Endpoint);
//             latestValue.Should().NotBeNull();
//             latestValue!
//                 .Value.Should()
//                 .Be("2");
//         }
//
//         [Fact]
//         public async Task Bind_PushMessagesInBatchToMultipleConsumers_OnlyOffsetOfCommittedBatchStored()
//         {
//             var e = new TestEventOne { Content = "Test" };
//             var fail = new TestEventOne { Content = "FAIL" };
//             var o1 = new TestOffset("a", "1");
//             var o2 = new TestOffset("a", "2");
//             var o3 = new TestOffset("a", "3");
//             var o4 = new TestOffset("a", "4");
//
//             _connector.Bind(
//                 TestConsumerEndpoint.GetDefault(),
//                 settings: new InboundConnectorSettings
//                 {
//                     Batch = new BatchSettings
//                     {
//                         Size = 2
//                     },
//                     Consumers = 2
//                 });
//             _broker.Connect();
//
//             _testSubscriber.FailCondition = m => m is TestEventOne m2 && m2.Content == "FAIL";
//
//             var consumer1 = (TestConsumer)_broker.Consumers[0];
//             var consumer2 = (TestConsumer)_broker.Consumers[1];
//
//             var tasks = new[]
//             {
//                 Task.Run(
//                     async () =>
//                     {
//                         try
//                         {
//                             await consumer1.TestHandleMessage(e, offset: o1);
//                             await consumer1.TestHandleMessage(e, offset: o2);
//                         }
//                         catch
//                         {
//                             // ignored
//                         }
//                     }),
//                 Task.Run(
//                     async () =>
//                     {
//                         try
//                         {
//                             await consumer2.TestHandleMessage(e, offset: o3);
//                             await consumer2.TestHandleMessage(fail, offset: o4);
//                         }
//                         catch
//                         {
//                             // ignored
//                         }
//                     })
//             };
//
//             await Task.WhenAll(tasks);
//
//             IComparableOffset? latestValue = await _scopedServiceProvider.GetRequiredService<IOffsetStore>()
//                 .GetLatestValue("a", consumer2.Endpoint);
//             latestValue.Should().NotBeNull();
//             latestValue!
//                 .Value.Should()
//                 .Be("2");
//         }
//     }
// }
