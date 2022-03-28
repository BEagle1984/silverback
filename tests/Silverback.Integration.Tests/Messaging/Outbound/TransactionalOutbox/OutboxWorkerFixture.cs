// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Tests.Integration.Messaging.Outbound.TransactionalOutbox;

public class OutboxWorkerFixture
{
    // TODO: REIMPLEMENT
    // private readonly TestProducerConfiguration _configuration1;
    //
    // private readonly TestProducerConfiguration _configuration2;
    //
    // private readonly OutboxMessageEndpoint _outboxEndpoint1;
    //
    // private readonly OutboxMessageEndpoint _outboxEndpoint2;
    //
    // public OutboxWorkerFixture()
    // {
    //     _configuration1 = new TestProducerConfiguration("topic1");
    //     _configuration2 = new TestProducerConfiguration("topic2");
    //
    //     TestProducerEndpoint endpoint1 = _configuration1.GetDefaultEndpoint();
    //     TestProducerEndpoint endpoint2 = _configuration2.GetDefaultEndpoint();
    //
    //     _outboxEndpoint1 = new OutboxMessageEndpoint(
    //         endpoint1.RawName,
    //         endpoint1.DisplayName,
    //         JsonSerializer.SerializeToUtf8Bytes(endpoint1));
    //     _outboxEndpoint2 = new OutboxMessageEndpoint(
    //         endpoint2.RawName,
    //         endpoint2.DisplayName,
    //         JsonSerializer.SerializeToUtf8Bytes(endpoint2));
    // }
    //
    // [Fact]
    // public async Task ProcessOutboxAsync_ShouldDequeueAndProduceMessages()
    // {
    //     InMemoryOutboxSettings outboxSettings = new();
    //     IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
    //         services => services
    //             .AddFakeLogger()
    //             .AddSilverback()
    //             .WithConnectionToMessageBroker(
    //                 options => options
    //                     .AddBroker<TestBroker>()
    //                     .AddInMemoryOutbox()
    //                     .AddOutboxWorker(new OutboxWorkerSettings(outboxSettings)))
    //             .AddEndpoints(
    //                 endpoints => endpoints
    //                     .AddOutbound<TestEventOne>(_configuration1)
    //                     .AddOutbound<TestEventTwo>(_configuration2)));
    //     IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
    //     IOutboxWriter outboxWriter = writerFactory.GetWriter(outboxSettings);
    //     InMemoryStorageFactory storageFactory = serviceProvider.GetRequiredService<InMemoryStorageFactory>();
    //     InMemoryStorage<OutboxMessage> storage = storageFactory.GetStorage<OutboxSettings, OutboxMessage>(outboxSettings);
    //     TestBroker broker = serviceProvider.GetRequiredService<TestBroker>();
    //     IOutboxWorker outboxWorker = serviceProvider.GetServices<IHostedService>().OfType<OutboxWorkerService>().Single().OutboxWorker;
    //
    //     await broker.ConnectAsync();
    //
    //     await outboxWriter.AddAsync(
    //         new OutboxMessage(
    //             typeof(TestEventOne),
    //             new byte[] { 0x01 },
    //             new[] { new MessageHeader("#", 1) },
    //             _outboxEndpoint1));
    //     await outboxWriter.AddAsync(
    //         new OutboxMessage(
    //             typeof(TestEventTwo),
    //             new byte[] { 0x02 },
    //             new[] { new MessageHeader("#", 2) },
    //             _outboxEndpoint2));
    //     await outboxWriter.AddAsync(
    //         new OutboxMessage(
    //             typeof(TestEventOne),
    //             new byte[] { 0x03 },
    //             new[] { new MessageHeader("#", 3) },
    //             _outboxEndpoint1));
    //
    //     storage.ItemsCount.Should().Be(3);
    //
    //     await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
    //
    //     broker.ProducedMessages.Should().HaveCount(3);
    //     broker.ProducedMessages[0].Endpoint.RawName.Should().Be("topic1");
    //     broker.ProducedMessages[0].Message.Should().BeEquivalentTo(new byte[] { 0x01 });
    //     broker.ProducedMessages[0].Headers.Should().ContainEquivalentOf(new MessageHeader("#", 1));
    //     broker.ProducedMessages[1].Endpoint.RawName.Should().Be("topic2");
    //     broker.ProducedMessages[1].Message.Should().BeEquivalentTo(new byte[] { 0x02 });
    //     broker.ProducedMessages[1].Headers.Should().ContainEquivalentOf(new MessageHeader("#", 2));
    //     broker.ProducedMessages[2].Endpoint.RawName.Should().Be("topic1");
    //     broker.ProducedMessages[2].Message.Should().BeEquivalentTo(new byte[] { 0x03 });
    //     broker.ProducedMessages[2].Headers.Should().ContainEquivalentOf(new MessageHeader("#", 3));
    //
    //     storage.ItemsCount.Should().Be(0);
    // }
    //
    // [Fact]
    // public async Task ProcessOutboxAsync_ShouldProcessSpecificOutbox_WhenMultipleOutboxesAreUsed()
    // {
    //     InMemoryOutboxSettings outboxSettings1 = new("outbox1");
    //     InMemoryOutboxSettings outboxSettings2 = new("outbox2");
    //     IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
    //         services => services
    //             .AddFakeLogger()
    //             .AddSilverback()
    //             .WithConnectionToMessageBroker(
    //                 options => options
    //                     .AddBroker<TestBroker>()
    //                     .AddInMemoryOutbox()
    //                     .AddOutboxWorker(new OutboxWorkerSettings(outboxSettings1))
    //                     .AddOutboxWorker(new OutboxWorkerSettings(outboxSettings2)))
    //             .AddEndpoints(
    //                 endpoints => endpoints
    //                     .AddOutbound<TestEventOne>(_configuration1)
    //                     .AddOutbound<TestEventTwo>(_configuration2)));
    //     IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
    //     IOutboxWriter outboxWriter1 = writerFactory.GetWriter(outboxSettings1);
    //     IOutboxWriter outboxWriter2 = writerFactory.GetWriter(outboxSettings2);
    //     TestBroker broker = serviceProvider.GetRequiredService<TestBroker>();
    //     IOutboxWorker[] outboxWorkers =
    //         serviceProvider.GetServices<IHostedService>().OfType<OutboxWorkerService>().Select(service => service.OutboxWorker).ToArray();
    //
    //     await broker.ConnectAsync();
    //
    //     await outboxWriter1.AddAsync(
    //         new OutboxMessage(
    //             typeof(TestEventOne),
    //             new byte[] { 0x01 },
    //             new[] { new MessageHeader("#", 1) },
    //             _outboxEndpoint1));
    //     await outboxWriter2.AddAsync(
    //         new OutboxMessage(
    //             typeof(TestEventTwo),
    //             new byte[] { 0x02 },
    //             new[] { new MessageHeader("#", 2) },
    //             _outboxEndpoint2));
    //
    //     await outboxWorkers[0].ProcessOutboxAsync(CancellationToken.None);
    //
    //     broker.ProducedMessages.Should().HaveCount(1);
    //     broker.ProducedMessages[0].Endpoint.RawName.Should().Be("topic1");
    //     broker.ProducedMessages[0].Message.Should().BeEquivalentTo(new byte[] { 0x01 });
    //     broker.ProducedMessages[0].Headers.Should().ContainEquivalentOf(new MessageHeader("#", 1));
    //
    //     await outboxWorkers[1].ProcessOutboxAsync(CancellationToken.None);
    //
    //     broker.ProducedMessages.Should().HaveCount(2);
    //     broker.ProducedMessages[1].Endpoint.RawName.Should().Be("topic2");
    //     broker.ProducedMessages[1].Message.Should().BeEquivalentTo(new byte[] { 0x02 });
    //     broker.ProducedMessages[1].Headers.Should().ContainEquivalentOf(new MessageHeader("#", 2));
    // }
    //
    // [Fact]
    // public async Task ProcessOutboxAsync_ShouldProduceToCorrectEndpoint_WhenMultipleEndpointsAreConfiguredForTheSameType()
    // {
    //     InMemoryOutboxSettings outboxSettings = new();
    //     IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
    //         services => services
    //             .AddFakeLogger()
    //             .AddSilverback()
    //             .WithConnectionToMessageBroker(
    //                 options => options
    //                     .AddBroker<TestBroker>()
    //                     .AddInMemoryOutbox()
    //                     .AddOutboxWorker(new OutboxWorkerSettings(outboxSettings)))
    //             .AddEndpoints(
    //                 endpoints => endpoints
    //                     .AddOutbound<TestEventOne>(_configuration1)
    //                     .AddOutbound<TestEventOne>(_configuration2)));
    //     IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
    //     IOutboxWriter outboxWriter = writerFactory.GetWriter(outboxSettings);
    //     TestBroker broker = serviceProvider.GetRequiredService<TestBroker>();
    //     IOutboxWorker outboxWorker = serviceProvider.GetServices<IHostedService>().OfType<OutboxWorkerService>().Single().OutboxWorker;
    //
    //     await broker.ConnectAsync();
    //
    //     await outboxWriter.AddAsync(
    //         new OutboxMessage(
    //             typeof(TestEventOne),
    //             new byte[] { 0x01 },
    //             new[] { new MessageHeader("#", 1) },
    //             _outboxEndpoint1));
    //     await outboxWriter.AddAsync(
    //         new OutboxMessage(
    //             typeof(TestEventOne),
    //             new byte[] { 0x02 },
    //             new[] { new MessageHeader("#", 2) },
    //             _outboxEndpoint2));
    //
    //     await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
    //
    //     broker.ProducedMessages.Should().HaveCount(2);
    //     broker.ProducedMessages[0].Endpoint.RawName.Should().Be("topic1");
    //     broker.ProducedMessages[0].Message.Should().BeEquivalentTo(new byte[] { 0x01 });
    //     broker.ProducedMessages[0].Headers.Should().ContainEquivalentOf(new MessageHeader("#", 1));
    //     broker.ProducedMessages[1].Endpoint.RawName.Should().Be("topic2");
    //     broker.ProducedMessages[1].Message.Should().BeEquivalentTo(new byte[] { 0x02 });
    //     broker.ProducedMessages[1].Headers.Should().ContainEquivalentOf(new MessageHeader("#", 2));
    // }
    //
    // [Fact]
    // public async Task ProcessOutboxAsync_ShouldProduceNewMessagesWhenCalledAgain()
    // {
    //     InMemoryOutboxSettings outboxSettings = new();
    //     IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
    //         services => services
    //             .AddFakeLogger()
    //             .AddSilverback()
    //             .WithConnectionToMessageBroker(
    //                 options => options
    //                     .AddBroker<TestBroker>()
    //                     .AddInMemoryOutbox()
    //                     .AddOutboxWorker(new OutboxWorkerSettings(outboxSettings)))
    //             .AddEndpoints(
    //                 endpoints => endpoints
    //                     .AddOutbound<TestEventOne>(_configuration1)
    //                     .AddOutbound<TestEventTwo>(_configuration2)));
    //     IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
    //     IOutboxWriter outboxWriter = writerFactory.GetWriter(outboxSettings);
    //     TestBroker broker = serviceProvider.GetRequiredService<TestBroker>();
    //     IOutboxWorker outboxWorker = serviceProvider.GetServices<IHostedService>().OfType<OutboxWorkerService>().Single().OutboxWorker;
    //
    //     await broker.ConnectAsync();
    //
    //     await outboxWriter.AddAsync(
    //         new OutboxMessage(
    //             typeof(TestEventOne),
    //             new byte[] { 0x01 },
    //             new[] { new MessageHeader("#", 1) },
    //             _outboxEndpoint1));
    //
    //     await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
    //
    //     broker.ProducedMessages.Should().HaveCount(1);
    //     broker.ProducedMessages[0].Message.Should().BeEquivalentTo(new byte[] { 0x01 });
    //
    //     await outboxWriter.AddAsync(
    //         new OutboxMessage(
    //             typeof(TestEventTwo),
    //             new byte[] { 0x02 },
    //             new[] { new MessageHeader("#", 2) },
    //             _outboxEndpoint2));
    //
    //     await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
    //
    //     broker.ProducedMessages.Should().HaveCount(2);
    //     broker.ProducedMessages[1].Message.Should().BeEquivalentTo(new byte[] { 0x02 });
    // }
    //
    // [Fact]
    // public async Task ProcessOutboxAsync_ShouldProduceSingleBatch()
    // {
    //     InMemoryOutboxSettings outboxSettings = new();
    //     IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
    //         services => services
    //             .AddFakeLogger()
    //             .AddSilverback()
    //             .WithConnectionToMessageBroker(
    //                 options => options
    //                     .AddBroker<TestBroker>()
    //                     .AddInMemoryOutbox()
    //                     .AddOutboxWorker(
    //                         new OutboxWorkerSettings(outboxSettings)
    //                         {
    //                             BatchSize = 5
    //                         }))
    //             .AddEndpoints(
    //                 endpoints => endpoints
    //                     .AddOutbound<TestEventOne>(_configuration1)));
    //     IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
    //     IOutboxWriter outboxWriter = writerFactory.GetWriter(outboxSettings);
    //     TestBroker broker = serviceProvider.GetRequiredService<TestBroker>();
    //     IOutboxWorker outboxWorker = serviceProvider.GetServices<IHostedService>().OfType<OutboxWorkerService>().Single().OutboxWorker;
    //
    //     await broker.ConnectAsync();
    //
    //     for (int i = 0; i < 10; i++)
    //     {
    //         await outboxWriter.AddAsync(new OutboxMessage(typeof(TestEventOne), new byte[] { 0x01 }, null, _outboxEndpoint1));
    //     }
    //
    //     await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
    //
    //     broker.ProducedMessages.Should().HaveCount(5);
    // }
}
