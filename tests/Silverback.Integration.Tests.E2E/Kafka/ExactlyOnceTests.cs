// TODO: REIMPLEMENT

// // Copyright (c) 2020 Sergio Aquilini
// // This code is licensed under MIT license (see LICENSE file for details)
//
// using System.Threading.Tasks;
// using FluentAssertions;
// using Microsoft.Extensions.DependencyInjection;
// using Silverback.Configuration;
// using Silverback.Messaging.Configuration;
// using Silverback.Messaging.Messages;
// using Silverback.Messaging.Publishing;
// using Silverback.Tests.Integration.E2E.TestHost;
// using Silverback.Tests.Integration.E2E.TestTypes.Database;
// using Silverback.Tests.Integration.E2E.TestTypes.Kafka;
// using Silverback.Tests.Integration.E2E.TestTypes.Messages;
// using Xunit;
// using Xunit.Abstractions;
//
// namespace Silverback.Tests.Integration.E2E.Kafka;
//
// public class ExactlyOnceTests : KafkaTestFixture
// {
//     public ExactlyOnceTests(ITestOutputHelper testOutputHelper)
//         : base(testOutputHelper)
//     {
//     }
//
//     [Fact]
//     public async Task ExactlyOnce_InMemoryInboundLog_DuplicatedMessagesIgnored()
//     {
//         Host.ConfigureServices(
//                 services => services
//                     .AddLogging()
//                     .AddSilverback()
//                     .UseModel()
//                     .WithConnectionToMessageBroker(
//                         options => options
//                             .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
//                             .AddInMemoryInboundLog())
//                     .AddKafkaEndpoints(
//                         endpoints => endpoints
//                             .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
//                             .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
//                             .AddInbound(
//                                 consumer => consumer
//                                     .ConsumeFrom(DefaultTopicName)
//                                     .EnsureExactlyOnce(strategy => strategy.LogMessages())
//                                     .ConfigureClient(
//                                         configuration => configuration
//                                             .WithGroupId(DefaultConsumerGroupId)
//                                             .CommitOffsetEach(1))))
//                     .AddIntegrationSpyAndSubscriber())
//             .Run();
//
//         IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
//
//         await publisher.PublishAsync(new TestEventOne());
//         await publisher.PublishAsync(new TestEventWithUniqueKey { UniqueKey = "1" });
//         await publisher.PublishAsync(new TestEventWithUniqueKey { UniqueKey = "2" });
//         await publisher.PublishAsync(new TestEventWithUniqueKey { UniqueKey = "1" });
//         await publisher.PublishAsync(new TestEventWithUniqueKey { UniqueKey = "2" });
//         await publisher.PublishAsync(new TestEventOne());
//
//         await Helper.WaitUntilAllMessagesAreConsumedAsync();
//
//         Helper.Spy.InboundEnvelopes.Should().HaveCount(4);
//
//         Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
//         Helper.Spy.InboundEnvelopes[1].Message.Should().BeOfType<TestEventWithUniqueKey>();
//         Helper.Spy.InboundEnvelopes[1].Message.As<TestEventWithUniqueKey>().UniqueKey.Should().Be("1");
//         Helper.Spy.InboundEnvelopes[2].Message.Should().BeOfType<TestEventWithUniqueKey>();
//         Helper.Spy.InboundEnvelopes[2].Message.As<TestEventWithUniqueKey>().UniqueKey.Should().Be("2");
//         Helper.Spy.InboundEnvelopes[3].Message.Should().BeOfType<TestEventOne>();
//
//         DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(6);
//     }
//
//     [Fact]
//     public async Task ExactlyOnce_DbInboundLog_DuplicatedMessagesIgnored()
//     {
//         Host.ConfigureServices(
//                 services => services
//                     .AddLogging()
//                     .AddSilverback()
//                     .UseModel()
//                     .UseDbContext<TestDbContext>()
//                     .WithConnectionToMessageBroker(
//                         options => options
//                             .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
//                             .AddInboundLogDatabaseTable())
//                     .AddKafkaEndpoints(
//                         endpoints => endpoints
//                             .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
//                             .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
//                             .AddInbound(
//                                 consumer => consumer
//                                     .ConsumeFrom(DefaultTopicName)
//                                     .EnsureExactlyOnce(strategy => strategy.LogMessages())
//                                     .ConfigureClient(
//                                         configuration => configuration
//                                             .WithGroupId(DefaultConsumerGroupId)
//                                             .CommitOffsetEach(1))))
//                     .AddIntegrationSpyAndSubscriber())
//             .WithTestDbContext()
//             .Run();
//
//         IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
//         TestDbContext dbContext = Host.ScopedServiceProvider.GetRequiredService<TestDbContext>();
//
//         await publisher.PublishAsync(new TestEventOne());
//         await publisher.PublishAsync(new TestEventWithUniqueKey { UniqueKey = "1" });
//         await publisher.PublishAsync(new TestEventWithUniqueKey { UniqueKey = "2" });
//         await publisher.PublishAsync(new TestEventWithUniqueKey { UniqueKey = "1" });
//         await publisher.PublishAsync(new TestEventWithUniqueKey { UniqueKey = "2" });
//         await publisher.PublishAsync(new TestEventOne());
//
//         await Helper.WaitUntilAllMessagesAreConsumedAsync();
//
//         Helper.Spy.InboundEnvelopes.Should().HaveCount(4);
//
//         Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
//         Helper.Spy.InboundEnvelopes[1].Message.Should().BeOfType<TestEventWithUniqueKey>();
//         Helper.Spy.InboundEnvelopes[1].Message.As<TestEventWithUniqueKey>().UniqueKey.Should().Be("1");
//         Helper.Spy.InboundEnvelopes[2].Message.Should().BeOfType<TestEventWithUniqueKey>();
//         Helper.Spy.InboundEnvelopes[2].Message.As<TestEventWithUniqueKey>().UniqueKey.Should().Be("2");
//         Helper.Spy.InboundEnvelopes[3].Message.Should().BeOfType<TestEventOne>();
//
//         dbContext.InboundMessages.Should().HaveCount(4);
//         DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(6);
//     }
//
//     [Fact]
//     public async Task ExactlyOnce_InMemoryOffsetStore_DuplicatedMessagesIgnored()
//     {
//         Host.ConfigureServices(
//                 services => services
//                     .AddLogging()
//                     .AddSilverback()
//                     .UseModel()
//                     .WithConnectionToMessageBroker(
//                         options => options
//                             .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
//                             .AddInMemoryOffsetStore())
//                     .AddKafkaEndpoints(
//                         endpoints => endpoints
//                             .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
//                             .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
//                             .AddInbound(
//                                 consumer => consumer
//                                     .ConsumeFrom(DefaultTopicName)
//                                     .EnsureExactlyOnce(strategy => strategy.StoreOffsets())
//                                     .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
//                     .AddTransientBrokerCallbackHandler<ResetOffsetPartitionsAssignedCallbackHandler>()
//                     .AddIntegrationSpyAndSubscriber())
//             .Run();
//
//         IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
//
//         await publisher.PublishAsync(new TestEventOne { Content = "Message 1" });
//         await publisher.PublishAsync(new TestEventOne { Content = "Message 2" });
//         await publisher.PublishAsync(new TestEventOne { Content = "Message 3" });
//
//         await Helper.WaitUntilAllMessagesAreConsumedAsync();
//         Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
//
//         await Helper.Broker.DisconnectAsync();
//         await Helper.Broker.ConnectAsync();
//
//         await publisher.PublishAsync(new TestEventOne { Content = "Message 4" });
//
//         await Helper.WaitUntilAllMessagesAreConsumedAsync();
//         Helper.Spy.InboundEnvelopes.Should().HaveCount(4);
//     }
//
//     [Fact]
//     public async Task ExactlyOnce_DbOffsetStore_DuplicatedMessagesIgnored()
//     {
//         Host.ConfigureServices(
//                 services => services
//                     .AddLogging()
//                     .AddSilverback()
//                     .UseModel()
//                     .UseDbContext<TestDbContext>()
//                     .WithConnectionToMessageBroker(
//                         options => options
//                             .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
//                             .AddOffsetStoreDatabaseTable())
//                     .AddKafkaEndpoints(
//                         endpoints => endpoints
//                             .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
//                             .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
//                             .AddInbound(
//                                 consumer => consumer
//                                     .ConsumeFrom(DefaultTopicName)
//                                     .EnsureExactlyOnce(strategy => strategy.StoreOffsets())
//                                     .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
//                     .AddTransientBrokerCallbackHandler<ResetOffsetPartitionsAssignedCallbackHandler>()
//                     .AddIntegrationSpyAndSubscriber())
//             .WithTestDbContext()
//             .Run();
//
//         IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
//
//         await publisher.PublishAsync(new TestEventOne { Content = "Message 1" });
//         await publisher.PublishAsync(new TestEventOne { Content = "Message 2" });
//         await publisher.PublishAsync(new TestEventOne { Content = "Message 3" });
//
//         await Helper.WaitUntilAllMessagesAreConsumedAsync();
//         Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
//
//         await Helper.Broker.DisconnectAsync();
//         await Helper.Broker.ConnectAsync();
//
//         await publisher.PublishAsync(new TestEventOne { Content = "Message 4" });
//
//         await Helper.WaitUntilAllMessagesAreConsumedAsync();
//         Helper.Spy.InboundEnvelopes.Should().HaveCount(4);
//     }
// }
