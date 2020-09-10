// TODO: Migrate

// // Copyright (c) 2020 Sergio Aquilini
// // This code is licensed under MIT license (see LICENSE file for details)
//
// using System;
// using System.Diagnostics.CodeAnalysis;
// using System.Threading.Tasks;
// using FluentAssertions;
// using Microsoft.Data.Sqlite;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.DependencyInjection;
// using Silverback.Messaging;
// using Silverback.Messaging.Broker;
// using Silverback.Messaging.Configuration;
// using Silverback.Messaging.Encryption;
// using Silverback.Messaging.LargeMessages;
// using Silverback.Messaging.Messages;
// using Silverback.Messaging.Publishing;
// using Silverback.Messaging.Serialization;
// using Silverback.Tests.Integration.E2E.TestHost;
// using Silverback.Tests.Integration.E2E.TestTypes;
// using Silverback.Tests.Integration.E2E.TestTypes.Database;
// using Silverback.Tests.Integration.E2E.TestTypes.Messages;
// using Xunit;
//
// namespace Silverback.Tests.Integration.E2E.Connectors
// {
//     [Trait("Category", "E2E")]
//     [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Connection scoped to test method")]
//     public class OffsetStoredInboundConnectorTests : E2ETestFixture
//     {
//         private static readonly byte[] AesEncryptionKey =
//         {
//             0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e,
//             0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c
//         };
//
//         [Fact]
//         [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
//         public async Task DuplicatedMessages_ConsumedOnce()
//         {
//             var message1 = new TestEventOne
//             {
//                 Content = "Hello E2E!"
//             };
//             var headers1 = new MessageHeaderCollection();
//             byte[]? rawMessage1 = await Endpoint.DefaultSerializer.SerializeAsync(
//                 message1,
//                 headers1,
//                 MessageSerializationContext.Empty);
//
//             var message2 = new TestEventOne
//             {
//                 Content = "Hello E2E!"
//             };
//             var headers2 = new MessageHeaderCollection();
//             byte[]? rawMessage2 = await Endpoint.DefaultSerializer.SerializeAsync(
//                 message2,
//                 headers2,
//                 MessageSerializationContext.Empty);
//
//             await using var connection = new SqliteConnection("DataSource=:memory:");
//             connection.Open();
//
//             var serviceProvider = Host.ConfigureServices(
//                     services => services
//                         .AddLogging()
//                         .AddDbContext<TestDbContext>(
//                             options => options
//                                 .UseSqlite(connection))
//                         .AddSilverback()
//                         .UseModel()
//                         .WithConnectionToMessageBroker(
//                             options => options
//                                 .AddInMemoryBroker()
//                                 .AddInMemoryChunkStore()
//                                 .AddDbOffsetStoredInboundConnector())
//                         .AddEndpoints(
//                             endpoints => endpoints
//                                 .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
//                                 .AddInbound(
//                                     new KafkaConsumerEndpoint("test-e2e"),
//                                     policy => policy.Retry().MaxFailedAttempts(10)))
//                         .UseDbContext<TestDbContext>()
//                         .AddSingletonSubscriber<OutboundInboundSubscriber>())
//                 .Run();
//
//             using (var scope = Host.ServiceProvider.CreateScope())
//             {
//                 await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync();
//             }
//
//             var consumer = (InMemoryConsumer)serviceProvider.GetRequiredService<IBroker>().Consumers[0];
//             await consumer.Receive(rawMessage1, headers1, new InMemoryOffset("test-e2e", 1));
//             await consumer.Receive(rawMessage2, headers2, new InMemoryOffset("test-e2e", 2));
//             await consumer.Receive(rawMessage1, headers1, new InMemoryOffset("test-e2e", 1));
//             await consumer.Receive(rawMessage2, headers2, new InMemoryOffset("test-e2e", 2));
//
//             Subscriber.InboundEnvelopes.Count.Should().Be(2);
//             Subscriber.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message1);
//             Subscriber.InboundEnvelopes[1].Message.Should().BeEquivalentTo(message2);
//         }
//
//         [Fact]
//         public async Task RetryPolicy_RetriedMultipleTimes()
//         {
//             var message = new TestEventOne
//             {
//                 Content = "Hello E2E!"
//             };
//             var tryCount = 0;
//
//             await using var connection = new SqliteConnection("DataSource=:memory:");
//             connection.Open();
//
//             var serviceProvider = Host.ConfigureServices(
//                     services => services
//                         .AddLogging()
//                         .AddDbContext<TestDbContext>(
//                             options => options
//                                 .UseSqlite(connection))
//                         .AddSilverback()
//                         .UseModel()
//                         .WithConnectionToMessageBroker(
//                             options => options
//                                 .AddInMemoryBroker()
//                                 .AddInMemoryChunkStore()
//                                 .AddDbOffsetStoredInboundConnector())
//                         .AddEndpoints(
//                             endpoints => endpoints
//                                 .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
//                                 .AddInbound(
//                                     new KafkaConsumerEndpoint("test-e2e"),
//                                     policy => policy.Retry().MaxFailedAttempts(10)))
//                         .UseDbContext<TestDbContext>()
//                         .AddSingletonSubscriber<OutboundInboundSubscriber>()
//                         .AddDelegateSubscriber(
//                             (IIntegrationEvent _) =>
//                             {
//                                 tryCount++;
//                                 if (tryCount != 3)
//                                     throw new InvalidOperationException("Retry!");
//                             }))
//                 .Run();
//
//             using (var scope = Host.ServiceProvider.CreateScope())
//             {
//                 await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync();
//             }
//
//             var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
//             await publisher.PublishAsync(message);
//
//             Subscriber.InboundEnvelopes.Count.Should().Be(3);
//         }
//
//         [Fact]
//         [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
//         public async Task EncryptionAndChunkingWithRetries_RetriedMultipleTimes()
//         {
//             var message = new TestEventOne
//             {
//                 Content = "Hello E2E!"
//             };
//
//             var tryCount = 0;
//
//             await using var connection = new SqliteConnection("DataSource=:memory:");
//             connection.Open();
//
//             var serviceProvider = Host.ConfigureServices(
//                     services => services
//                         .AddLogging()
//                         .AddDbContext<TestDbContext>(
//                             options => options
//                                 .UseSqlite(connection))
//                         .AddSilverback()
//                         .UseModel()
//                         .WithConnectionToMessageBroker(
//                             options => options
//                                 .AddInMemoryBroker()
//                                 .AddInMemoryChunkStore()
//                                 .AddDbOffsetStoredInboundConnector())
//                         .AddEndpoints(
//                             endpoints => endpoints
//                                 .AddOutbound<IIntegrationEvent>(
//                                     new KafkaProducerEndpoint("test-e2e")
//                                     {
//                                         Chunk = new ChunkSettings
//                                         {
//                                             Size = 10
//                                         },
//                                         Encryption = new SymmetricEncryptionSettings
//                                         {
//                                             Key = AesEncryptionKey
//                                         }
//                                     })
//                                 .AddInbound(
//                                     new KafkaConsumerEndpoint("test-e2e")
//                                     {
//                                         Encryption = new SymmetricEncryptionSettings
//                                         {
//                                             Key = AesEncryptionKey
//                                         }
//                                     },
//                                     policy => policy.Retry().MaxFailedAttempts(10)))
//                         .UseDbContext<TestDbContext>()
//                         .AddSingletonSubscriber<OutboundInboundSubscriber>()
//                         .AddDelegateSubscriber(
//                             (IIntegrationEvent _) =>
//                             {
//                                 tryCount++;
//                                 if (tryCount != 3)
//                                     throw new InvalidOperationException("Retry!");
//                             }))
//                 .Run();
//
//             using (var scope = Host.ServiceProvider.CreateScope())
//             {
//                 await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync();
//             }
//
//             var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
//             await publisher.PublishAsync(message);
//
//             Subscriber.InboundEnvelopes.Count.Should().Be(3);
//         }
//     }
// }
