// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Tests.Integration.Messaging.Producing.TransactionalOutbox;

public class OutboxWorkerServiceFixture
{
    // TODO: REIMPLEMENT
    // private readonly TestProducerConfiguration _configuration;
    //
    // private readonly OutboxMessageEndpoint _outboxEndpoint;
    //
    // public OutboxWorkerServiceFixture()
    // {
    //     _configuration = new TestProducerConfiguration("topic1");
    //
    //     TestProducerEndpoint endpoint = _configuration.GetDefaultEndpoint();
    //
    //     _outboxEndpoint = new OutboxMessageEndpoint(
    //         endpoint.RawName,
    //         endpoint.DisplayName,
    //         JsonSerializer.SerializeToUtf8Bytes(endpoint));
    // }
    //
    // [Fact]
    // public async Task ProcessOutboxAsync_ShouldProduceAllMessagesInMultipleBatches()
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
    //                             BatchSize = 10,
    //                             Interval = TimeSpan.FromMinutes(1)
    //                         }))
    //             .AddEndpoints(
    //                 endpoints => endpoints
    //                     .AddOutbound<TestEventOne>(_configuration)));
    //     IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
    //     IOutboxWriter outboxWriter = writerFactory.GetWriter(outboxSettings);
    //     TestBroker broker = serviceProvider.GetRequiredService<TestBroker>();
    //     OutboxWorkerService service = serviceProvider.GetServices<IHostedService>().OfType<OutboxWorkerService>().Single();
    //
    //     await broker.ConnectAsync();
    //
    //     for (int i = 0; i < 42; i++)
    //     {
    //         await outboxWriter.AddAsync(new OutboxMessage(typeof(TestEventOne), new byte[] { 0x01 }, null, _outboxEndpoint));
    //     }
    //
    //     CancellationTokenSource cancellationTokenSource = new();
    //     await service.StartAsync(cancellationTokenSource.Token);
    //
    //     await AsyncTestingUtil.WaitAsync(() => broker.ProducedMessages.Count == 42);
    //
    //     cancellationTokenSource.Cancel();
    //
    //     broker.ProducedMessages.Should().HaveCount(42);
    // }
}
