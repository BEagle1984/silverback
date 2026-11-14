// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Storage;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestHost.Database;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class OutboxInMemoryTests : KafkaTests
{
    public OutboxInMemoryTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Outbox_ShouldProduceMessages(bool waitForCommit)
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka()
                .AddInMemoryOutbox()
                .AddOutboxWorker(worker => worker
                    .ProcessOutbox(outbox => outbox.UseMemory())
                    .WithInterval(TimeSpan.FromMilliseconds(50))))
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

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 0; i < 3; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        if (waitForCommit)
        {
            await Helper.WaitUntilAllMessagesAreCommittedAsync();
            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(3);
        }
        else
        {
            await Helper.WaitUntilAllMessagesAreConsumedAsync();
        }

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .ShouldBe(Enumerable.Range(0, 3).Select(i => $"{i}"), true);
    }

    [Fact]
    public async Task Outbox_ShouldContinueTraceOfStoredMessage()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka()
                .AddInMemoryOutbox()
                .AddOutboxWorker(worker => worker
                    .ProcessOutbox(outbox => outbox.UseMemory())
                    .WithInterval(TimeSpan.FromMilliseconds(50))))
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

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        string expectedTraceId;

        using (Activity activity = new("TestActivity"))
        {
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.Start();
            expectedTraceId = activity.TraceId.ToString();

            await publisher.PublishEventAsync(new TestEventOne());
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(1);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        ActivityContext storedContext = ActivityContext.Parse(Helper.Spy.OutboundEnvelopes[0].Headers[DefaultMessageHeaders.TraceId]!, null);
        MessageHeader traceIdHeader = Helper.Spy.InboundEnvelopes[0].Headers
            .Where(header => header.Name == DefaultMessageHeaders.TraceId)
            .ShouldHaveSingleItem();
        ActivityContext producedContext = ActivityContext.Parse(traceIdHeader.Value!, null);
        storedContext.TraceId.ToString().ShouldBe(expectedTraceId);
        producedContext.TraceId.ToString().ShouldBe(expectedTraceId);

        // The worker produces the message with its own span, which continues the stored trace
        producedContext.SpanId.ShouldNotBe(storedContext.SpanId);
    }

    [Fact]
    public async Task Outbox_ShouldIgnoreTransaction()
    {
        using SqliteDatabase database = await SqliteDatabase.StartAsync();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka()
                .AddInMemoryOutbox()
                .AddOutboxWorker(worker => worker
                    .ProcessOutbox(outbox => outbox.UseMemory())
                    .WithInterval(TimeSpan.FromMilliseconds(50))))
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

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await using SqliteConnection connection = new(database.ConnectionString);
        await connection.OpenAsync();
        await using DbTransaction transaction = await connection.BeginTransactionAsync();

        await using IStorageTransaction storageTransaction = publisher.EnlistDbTransaction(transaction);

        await publisher.PublishEventAsync(new TestEventOne());
        await transaction.RollbackAsync();
        await connection.CloseAsync();

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(1);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        DefaultTopic.MessagesCount.ShouldBe(1);
    }

    [Fact]
    public async Task Outbox_ShouldProduceBatch()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka()
                .AddInMemoryOutbox()
                .AddOutboxWorker(worker => worker
                    .ProcessOutbox(outbox => outbox.UseMemory())
                    .WithInterval(TimeSpan.FromMilliseconds(50))))
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

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.WrapAndPublishBatchAsync([new TestEventOne(), new TestEventOne()]);
        await publisher.WrapAndPublishBatchAsync(new IIntegrationEvent[] { new TestEventTwo(), new TestEventOne() });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(4);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(4);
        Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventOne>>().Count().ShouldBe(3);
        Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventTwo>>().Count().ShouldBe(1);
    }

    [Fact]
    public async Task Outbox_ShouldProduceAsyncBatch()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka()
                .AddInMemoryOutbox()
                .AddOutboxWorker(worker => worker
                    .ProcessOutbox(outbox => outbox.UseMemory())
                    .WithInterval(TimeSpan.FromMilliseconds(50))))
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
            await Task.Delay(20);
            yield return new TestEventThree();
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.WrapAndPublishBatchAsync(GetMessagesAsync());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventOne>>().Count().ShouldBe(1);
        Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventTwo>>().Count().ShouldBe(1);
        Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventThree>>().Count().ShouldBe(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Outbox_ShouldThrow_WhenProducingBatchFails(bool asyncBatch)
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka()
                .AddInMemoryOutbox())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>(
                        "my-endpoint",
                        endpoint => endpoint
                            .ProduceTo(DefaultTopicName)
                            .AddHeader<TestEventOne>(
                                "x-test",
                                message => message?.ContentEventOne == "fail"
                                    ? throw new InvalidOperationException("Enrichment failed.")
                                    : "ok")
                            .StoreToOutbox(outbox => outbox.UseMemory())))));

        TestEventOne[] messages =
        [
            new() { ContentEventOne = "one" },
            new() { ContentEventOne = "fail" },
            new() { ContentEventOne = "three" }
        ];

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        // Run in a separate task, so that a synchronous hang makes the test fail instead of blocking it
        Exception? exception = await Record.ExceptionAsync(() => Task.Run(() => asyncBatch
                ? publisher.WrapAndPublishBatchAsync(messages.ToAsyncEnumerable())
                : publisher.WrapAndPublishBatchAsync(messages))
            .WaitAsync(TimeSpan.FromSeconds(10)));

        exception.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Enrichment failed.");
    }

    [Fact]
    public async Task Outbox_ShouldProduceBatch_WhenPublishingSynchronouslyWithSynchronizationContext()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka()
                .AddInMemoryOutbox()
                .AddOutboxWorker(worker => worker
                    .ProcessOutbox(outbox => outbox.UseMemory())
                    .WithInterval(TimeSpan.FromMilliseconds(50))))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>(
                        "my-endpoint",
                        endpoint => endpoint
                            .ProduceTo(DefaultTopicName)
                            .StoreToOutbox(outbox => outbox.UseMemory())))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await BlockedSynchronizationContext
            .RunAsync(() => publisher.WrapAndPublishBatch([new TestEventOne(), new TestEventOne()]))
            .WaitAsync(TimeSpan.FromSeconds(10));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
    }
}
