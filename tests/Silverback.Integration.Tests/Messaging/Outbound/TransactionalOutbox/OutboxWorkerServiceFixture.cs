// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Outbound.TransactionalOutbox;

public class OutboxWorkerServiceFixture
{
    private readonly TestProducerConfiguration _configuration;

    private readonly OutboxMessageEndpoint _outboxEndpoint;

    public OutboxWorkerServiceFixture()
    {
        _configuration = new TestProducerConfiguration("topic1");

        TestProducerEndpoint endpoint = _configuration.GetDefaultEndpoint();

        _outboxEndpoint = new OutboxMessageEndpoint(
            endpoint.RawName,
            endpoint.DisplayName,
            JsonSerializer.SerializeToUtf8Bytes(endpoint));
    }

    [Fact]
    public async Task ProcessOutboxAsync_ShouldProduceAllMessagesInMultipleBatches()
    {
        InMemoryOutboxSettings outboxSettings = new();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()
                        .AddInMemoryOutbox()
                        .AddOutboxWorker(
                            new OutboxWorkerSettings(outboxSettings)
                            {
                                BatchSize = 10,
                                Interval = TimeSpan.FromMinutes(1)
                            }))
                .AddEndpoints(
                    endpoints => endpoints
                        .AddOutbound<TestEventOne>(_configuration)));
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(outboxSettings);
        TestBroker broker = serviceProvider.GetRequiredService<TestBroker>();
        OutboxWorkerService service = serviceProvider.GetServices<IHostedService>().OfType<OutboxWorkerService>().Single();

        await broker.ConnectAsync();

        for (int i = 0; i < 42; i++)
        {
            await outboxWriter.AddAsync(new OutboxMessage(typeof(TestEventOne), new byte[] { 0x01 }, null, _outboxEndpoint));
        }

        CancellationTokenSource cancellationTokenSource = new();
        await service.StartAsync(cancellationTokenSource.Token);

        await AsyncTestingUtil.WaitAsync(() => broker.ProducedMessages.Count == 42);

        cancellationTokenSource.Cancel();

        broker.ProducedMessages.Should().HaveCount(42);
    }
}
