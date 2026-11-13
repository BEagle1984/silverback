// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Messaging.Publishing;
using Silverback.Storage;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestHost.Database;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

[SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Test code")]
public class OutboxTestingTests : KafkaTests
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromMilliseconds(300);

    public OutboxTestingTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task OutboxHelper_ShouldGetPendingMessagesWithoutRemovingThem()
    {
        await ConfigureInMemoryAsync(options => options.UseManualOutboxWorker());

        await PublishAsync(3);

        IReadOnlyList<OutboxMessage> messages = await Helper.Outbox.GetMessagesAsync();

        messages.Count.ShouldBe(3);
        messages.ShouldAllBe(message => message.EndpointName == "my-endpoint");
        (await Helper.Outbox.GetLengthAsync()).ShouldBe(3);
        (await Helper.Outbox.IsEmptyAsync()).ShouldBeFalse();
        (await Helper.Outbox.GetMessagesAsync()).Count.ShouldBe(3);
        DefaultTopic.MessagesCount.ShouldBe(0);
    }

    [Fact]
    public async Task OutboxHelper_ShouldClearOutbox()
    {
        await ConfigureInMemoryAsync(options => options.UseManualOutboxWorker());

        await PublishAsync(3);

        await Helper.Outbox.ClearAsync();

        (await Helper.Outbox.IsEmptyAsync()).ShouldBeTrue();

        // Nothing is left to be produced
        await Helper.Outbox.ProcessAsync();
        DefaultTopic.MessagesCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OutboxHelper_ShouldProcessOutboxOnDemand_WhenUsingManualOutboxWorker(bool manualWorkerFirst)
    {
        await ConfigureInMemoryAsync(options => options.UseManualOutboxWorker(), manualWorkerFirst);

        await PublishAsync(3);

        await Task.Delay(IdleDelay);

        // The messages are not produced until the processing is triggered
        (await Helper.Outbox.GetLengthAsync()).ShouldBe(3);
        DefaultTopic.MessagesCount.ShouldBe(0);

        await Helper.Outbox.ProcessAsync(new InMemoryOutboxSettings());

        (await Helper.Outbox.IsEmptyAsync()).ShouldBeTrue();

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .ShouldBe(Enumerable.Range(0, 3).Select(i => $"{i}"), true);
    }

    [Fact]
    public async Task OutboxHelper_ShouldPauseAndResumeWorker()
    {
        await ConfigureInMemoryAsync(null);

        await Helper.Outbox.PauseWorkerAsync();

        await PublishAsync(2);
        await Task.Delay(IdleDelay);

        (await Helper.Outbox.GetLengthAsync()).ShouldBe(2);
        DefaultTopic.MessagesCount.ShouldBe(0);

        Helper.Outbox.ResumeWorker();

        await Helper.Outbox.WaitUntilEmptyAsync();
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
    }

    [Fact]
    public async Task OutboxHelper_ShouldThrow_WhenProcessingWithoutWorker()
    {
        await ConfigureInMemoryAsync(null, includeWorker: false);

        await PublishAsync(1);

        await Should.ThrowAsync<InvalidOperationException>(async () => await Helper.Outbox.ProcessAsync());
        await Should.ThrowAsync<InvalidOperationException>(async () => await Helper.Outbox.PauseWorkerAsync());
    }

    [Fact]
    public async Task OutboxHelper_ShouldReadOutboxWithoutWorker()
    {
        await ConfigureInMemoryAsync(null, includeWorker: false);

        (await Helper.Outbox.IsEmptyAsync()).ShouldBeTrue();

        await PublishAsync(2);

        (await Helper.Outbox.IsEmptyAsync()).ShouldBeFalse();
        (await Helper.Outbox.GetMessagesAsync()).Count.ShouldBe(2);
        (await Helper.Outbox.GetLengthAsync(new InMemoryOutboxSettings())).ShouldBe(2);

        await Helper.Outbox.ClearAsync();

        (await Helper.Outbox.IsEmptyAsync()).ShouldBeTrue();
    }

    [Fact]
    public async Task BypassOutbox_ShouldProduceMessagesWithoutWorkerAndStorage()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka()
                .BypassOutbox())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(
                        "my-endpoint",
                        endpoint => endpoint
                            .ProduceTo(DefaultTopicName)
                            .StoreToOutbox(outbox => outbox.UseMemory())))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        await PublishAsync(3);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .ShouldBe(Enumerable.Range(0, 3).Select(i => $"{i}"), true);
        (await Helper.Outbox.IsEmptyAsync()).ShouldBeTrue();
        (await Helper.Outbox.GetMessagesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task BypassOutbox_ShouldProduceBatches()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka()
                .AddInMemoryOutbox()
                .AddOutboxWorker(worker => worker
                    .ProcessOutbox(outbox => outbox.UseMemory())
                    .WithInterval(TimeSpan.FromMilliseconds(50)))
                .BypassOutbox())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(
                        "my-endpoint",
                        endpoint => endpoint
                            .ProduceTo(DefaultTopicName)
                            .StoreToOutbox(outbox => outbox.UseMemory())))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        static async IAsyncEnumerable<IIntegrationEvent> GetMessagesAsync()
        {
            yield return new TestEventOne();
            await Task.Delay(10);
            yield return new TestEventTwo();
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.WrapAndPublishBatchAsync([new TestEventOne(), new TestEventOne()]);
        await publisher.WrapAndPublishBatchAsync(GetMessagesAsync());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(4);
        (await Helper.Outbox.IsEmptyAsync()).ShouldBeTrue();
    }

    [Fact]
    public async Task OutboxHelper_ShouldNotWaitForOutboxNotBeingDrained_WhenUsingManualOutboxWorker()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka()
                .AddInMemoryOutbox()
                .AddOutboxWorker(worker => worker
                    .ProcessOutbox(outbox => outbox.UseMemory())
                    .WithInterval(TimeSpan.FromMilliseconds(50)))
                .UseManualOutboxWorker())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>("my-endpoint", endpoint => endpoint.ProduceTo(DefaultTopicName))
                    .Produce<TestEventTwo>(
                        "outbox-endpoint",
                        endpoint => endpoint
                            .ProduceTo("other-topic")
                            .StoreToOutbox(outbox => outbox.UseMemory())))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber()
            .AddDelegateSubscriber<IInboundEnvelope<TestEventOne>, IPublisher>(
                async Task (_, publisher) => await publisher.PublishEventAsync(new TestEventTwo())));

        await Host.ServiceProvider.GetRequiredService<IPublisher>().PublishEventAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync(TimeSpan.FromSeconds(10)).AsTask().WaitAsync(TimeSpan.FromSeconds(15));
        await Helper.WaitUntilOutboxIsEmptyAsync(TimeSpan.FromSeconds(5));

        (await Helper.Outbox.GetMessagesAsync()).Count.ShouldBe(1);
        (await Helper.IsOutboxEmptyAsync()).ShouldBeTrue();
    }

    [Fact]
    public async Task OutboxHelper_ShouldNotWait_WhenStoringToOutboxWithoutWorker()
    {
        await ConfigureInMemoryAsync(null, includeWorker: false);

        await PublishAsync(2);

        await Helper.WaitUntilOutboxIsEmptyAsync(TimeSpan.FromSeconds(5));
        await Helper.WaitUntilAllMessagesAreConsumedAsync(TimeSpan.FromSeconds(5));

        (await Helper.IsOutboxEmptyAsync()).ShouldBeTrue();
        (await Helper.Outbox.GetLengthAsync()).ShouldBe(2);
    }

    [Fact]
    public async Task OutboxHelper_ShouldThrow_WhenProcessingMessagesThatCannotBeProduced()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka()
                .AddInMemoryOutbox()
                .AddOutboxWorker(worker => worker
                    .ProcessOutbox(outbox => outbox.UseMemory())
                    .DisableMessageOrderEnforcement())
                .UseManualOutboxWorker())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(
                        "my-endpoint",
                        endpoint => endpoint
                            .ProduceTo(DefaultTopicName)
                            .StoreToOutbox(outbox => outbox.UseMemory())))));

        IOutboxWriter writer = Host.ServiceProvider
            .GetRequiredService<OutboxWriterFactory>()
            .GetWriter(new InMemoryOutboxSettings(), Host.ServiceProvider);
        await writer.AddAsync(new OutboxMessage([1], null, "unknown-endpoint"));

        using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(10));

        await Should.ThrowAsync<OutboxProcessingException>(async () => await Helper.Outbox.ProcessAsync(cancellationTokenSource.Token));
    }

    [Fact]
    public async Task OutboxHelper_ShouldThrow_WhenProcessingMessagesThatCannotBeProducedWithActiveWorker()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka()
                .AddInMemoryOutbox()
                .AddOutboxWorker(worker => worker
                    .ProcessOutbox(outbox => outbox.UseMemory())
                    .WithInterval(TimeSpan.FromMilliseconds(10))
                    .DisableMessageOrderEnforcement()))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(
                        "my-endpoint",
                        endpoint => endpoint
                            .ProduceTo(DefaultTopicName)
                            .StoreToOutbox(outbox => outbox.UseMemory())))));

        IOutboxWriter writer = Host.ServiceProvider
            .GetRequiredService<OutboxWriterFactory>()
            .GetWriter(new InMemoryOutboxSettings(), Host.ServiceProvider);
        await writer.AddAsync(new OutboxMessage([1], null, "unknown-endpoint"));

        using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(10));

        await Should.ThrowAsync<OutboxProcessingException>(async () => await Helper.Outbox.ProcessAsync(cancellationTokenSource.Token));
    }

    [Fact]
    public async Task OutboxHelper_ShouldNotThrow_WhenOutboxGrowsWhileProcessing()
    {
        TaskCompletionSource replyStored = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options =>
            {
                options
                    .AddMockedKafka()
                    .AddInMemoryOutbox()
                    .AddOutboxWorker(worker => worker
                        .ProcessOutbox(outbox => outbox.UseMemory())
                        .WithBatchSize(1))
                    .UseManualOutboxWorker();

                // The second pass starts only once the first produced message has made a consumer store another one to the outbox,
                // to deterministically have the outbox growing while it is being drained
                OutboxReaderFactory readerFactory = options.SilverbackBuilder.Services.GetSingletonServiceInstance<OutboxReaderFactory>()!;
                InMemoryOutboxFactory outboxFactory = options.SilverbackBuilder.Services.GetSingletonServiceInstance<InMemoryOutboxFactory>()!;
                readerFactory.OverrideFactories(
                    (settings, _) => new GatedOutboxReader(new InMemoryOutboxReader(outboxFactory.GetOutbox(settings)), replyStored.Task));
            })
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>(
                        "outbox-one",
                        endpoint => endpoint
                            .ProduceTo("topic-a")
                            .StoreToOutbox(outbox => outbox.UseMemory()))
                    .Produce<TestEventTwo>(
                        "outbox-two",
                        endpoint => endpoint
                            .ProduceTo("topic-b")
                            .StoreToOutbox(outbox => outbox.UseMemory())))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom("topic-a", "topic-b"))))
            .AddIntegrationSpyAndSubscriber()
            .AddDelegateSubscriber<IInboundEnvelope<TestEventOne>, IPublisher>(
                async Task (_, publisher) =>
                {
                    await publisher.PublishEventAsync(new TestEventTwo());
                    replyStored.TrySetResult();
                }));

        await PublishAsync(20);

        // Each produced message makes a consumer store another message to the outbox while it is being drained
        using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(30));
        await Helper.Outbox.ProcessAsync(cancellationTokenSource.Token);

        await Helper.WaitUntilAllMessagesAreConsumedAsync(TimeSpan.FromSeconds(10), "topic-a");
        await Helper.Outbox.ProcessAsync(cancellationTokenSource.Token);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventOne>>().Count().ShouldBe(20);
        Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventTwo>>().Count().ShouldBe(20);
        (await Helper.Outbox.IsEmptyAsync()).ShouldBeTrue();
    }

    [Fact]
    public async Task BypassOutbox_ShouldProduceToDynamicEndpoint()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka()
                .BypassOutbox())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>(
                        "my-dynamic-endpoint",
                        endpoint => endpoint
                            .ProduceTo(message => message?.ContentEventOne == "1" ? "topic1" : "topic2")
                            .StoreToOutbox(outbox => outbox.UseMemory())))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom("topic1", "topic2"))))
            .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes.Count(envelope => envelope.Endpoint.RawName == "topic1").ShouldBe(1);
        Helper.Spy.InboundEnvelopes.Count(envelope => envelope.Endpoint.RawName == "topic2").ShouldBe(1);
    }

    [Fact]
    public async Task OutboxHelper_ShouldGetAndClearMessages_WhenUsingSqlite()
    {
        using SqliteDatabase database = await SqliteDatabase.StartAsync();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .InitDatabase(storageInitializer => storageInitializer.CreateSqliteOutboxAsync(database.ConnectionString))
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka()
                .AddSqliteOutbox()
                .AddOutboxWorker(worker => worker
                    .ProcessOutbox(outbox => outbox.UseSqlite(database.ConnectionString))
                    .WithInterval(TimeSpan.FromMilliseconds(50)))
                .UseManualOutboxWorker())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(
                        "my-endpoint",
                        endpoint => endpoint
                            .ProduceTo(DefaultTopicName)
                            .StoreToOutbox(outbox => outbox.UseSqlite(database.ConnectionString))))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        await PublishAsync(3);

        (await Helper.Outbox.GetMessagesAsync()).Count.ShouldBe(3);
        (await Helper.Outbox.GetLengthAsync()).ShouldBe(3);

        await Helper.Outbox.ClearAsync();

        (await Helper.Outbox.IsEmptyAsync()).ShouldBeTrue();

        await PublishAsync(2);
        await Helper.Outbox.ProcessAsync();
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
    }

    private ValueTask ConfigureInMemoryAsync(
        Func<BrokerOptionsBuilder, BrokerOptionsBuilder>? additionalConfiguration,
        bool manualWorkerFirst = false,
        bool includeWorker = true) =>
        Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options =>
            {
                options.AddMockedKafka().AddInMemoryOutbox();

                if (manualWorkerFirst)
                    additionalConfiguration?.Invoke(options);

                if (includeWorker)
                {
                    options.AddOutboxWorker(worker => worker
                        .ProcessOutbox(outbox => outbox.UseMemory())
                        .WithInterval(TimeSpan.FromMilliseconds(50)));
                }

                if (!manualWorkerFirst)
                    additionalConfiguration?.Invoke(options);
            })
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(
                        "my-endpoint",
                        endpoint => endpoint
                            .ProduceTo(DefaultTopicName)
                            .StoreToOutbox(outbox => outbox.UseMemory())))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

    private async Task PublishAsync(int count)
    {
        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 0; i < count; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }
    }

    private sealed class GatedOutboxReader : IOutboxReader
    {
        private readonly IOutboxReader _inner;

        private readonly Task _gate;

        private int _getCalls;

        public GatedOutboxReader(IOutboxReader inner, Task gate)
        {
            _inner = inner;
            _gate = gate;
        }

        public async Task<IDisposableAsyncEnumerable<OutboxMessage>> GetAsync(int count)
        {
            // Hold the second read until the gate is opened
            if (Interlocked.Increment(ref _getCalls) == 2)
                await _gate.WaitAsync(TimeSpan.FromSeconds(20));

            return await _inner.GetAsync(count);
        }

        public Task<int> GetLengthAsync() => _inner.GetLengthAsync();

        public Task<TimeSpan> GetMaxAgeAsync() => _inner.GetMaxAgeAsync();

        public Task AcknowledgeAsync(IEnumerable<OutboxMessage> outboxMessages) => _inner.AcknowledgeAsync(outboxMessages);
    }
}
