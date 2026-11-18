// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Logging;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ProducerEndpointTests
{
    public enum ProduceMethod
    {
        PublishAsync,
        Produce,
        ProduceWithCallbacks
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldTagActivityWithKafkaKeyAndOffset()
    {
        TestingCollection<Activity> stoppedActivities = [];
        using ActivityListener listener = new();
        listener.ShouldListenTo = source => source.Name == "Silverback.Integration.Produce";
        listener.Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
        listener.ActivityStopped = stoppedActivities.Add;
        ActivitySource.AddActivityListener(listener);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer.Produce<TestEventOne>(endpoint => endpoint
                    .ProduceTo(DefaultTopicName)
                    .SetKafkaKey(message => message?.ContentEventOne)))));

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        ActivityTraceId traceId;

        using (Activity activity = new("TestActivity"))
        {
            activity.Start();
            traceId = activity.TraceId;

            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
        }

        Activity produceActivity = stoppedActivities.Where(activity => activity.TraceId == traceId).ShouldHaveSingleItem();
        produceActivity.GetTagItem("messaging.destination").ShouldBe(DefaultTopicName);
        produceActivity.GetTagItem("messaging.message_id").ShouldBe($"{DefaultTopicName}[0]@0");
        produceActivity.GetTagItem("messaging.kafka.partition").ShouldBe($"{DefaultTopicName}[0]");
        produceActivity.GetTagItem("messaging.kafka.message_key").ShouldBe("one");
    }

    [Theory]
    [InlineData(ProduceMethod.PublishAsync)]
    [InlineData(ProduceMethod.Produce)]
    [InlineData(ProduceMethod.ProduceWithCallbacks)]
    public async Task ProducerEndpoint_ShouldIncludeActivityTagsInProducedLogScope(ProduceMethod produceMethod)
    {
        TestingCollection<Activity> stoppedActivities = [];
        using ActivityListener listener = new();
        listener.ShouldListenTo = source => source.Name == "Silverback.Integration.Produce";
        listener.Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
        listener.ActivityStopped = stoppedActivities.Add;
        ActivitySource.AddActivityListener(listener);

        LogScopesCollector logScopesCollector = new(IntegrationLogEvents.MessageProduced);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging(logging => logging.AddProvider(logScopesCollector).Configure(LogScopesCollector.TrackActivity))
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer.Produce<TestEventOne>(endpoint => endpoint
                    .ProduceTo(DefaultTopicName)
                    .SetKafkaKey(message => message?.ContentEventOne)))));

        using Activity parentActivity = new("TestActivity");
        parentActivity.Start();

        await ProduceAsync(produceMethod, new TestEventOne { ContentEventOne = "one" });

        await AsyncTestingUtil.WaitAsync(() => logScopesCollector.Scopes.Count >= 1 && stoppedActivities.Count >= 1);

        ActivityTraceId traceId = parentActivity.TraceId;
        Activity produceActivity = stoppedActivities.Where(activity => activity.TraceId == traceId).ShouldHaveSingleItem();
        produceActivity.GetTagItem("messaging.message_id").ShouldBe($"{DefaultTopicName}[0]@0");
        produceActivity.GetTagItem("messaging.kafka.partition").ShouldBe($"{DefaultTopicName}[0]");
        produceActivity.GetTagItem("messaging.kafka.message_key").ShouldBe("one");

        IReadOnlyDictionary<string, object?> scope = logScopesCollector.Scopes.ShouldHaveSingleItem();
        scope["SpanId"].ShouldBe(produceActivity.SpanId.ToHexString());
        scope["messaging.message_id"].ShouldBe($"{DefaultTopicName}[0]@0");
        scope["messaging.kafka.partition"].ShouldBe($"{DefaultTopicName}[0]");
        scope["messaging.kafka.message_key"].ShouldBe("one");

        // The caller's activity is neither tagged nor replaced
        Activity.Current.ShouldBe(parentActivity);
        parentActivity.GetTagItem("messaging.kafka.message_key").ShouldBeNull();
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldIncludeActivityTagsInProducedLogScope_WhenChunkedAndProducingWithCallbacks()
    {
        TestingCollection<Activity> stoppedActivities = [];
        using ActivityListener listener = new();
        listener.ShouldListenTo = source => source.Name == "Silverback.Integration.Produce";
        listener.Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
        listener.ActivityStopped = stoppedActivities.Add;
        ActivitySource.AddActivityListener(listener);

        LogScopesCollector logScopesCollector = new(IntegrationLogEvents.MessageProduced);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging(logging => logging.AddProvider(logScopesCollector).Configure(LogScopesCollector.TrackActivity))
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer.Produce<TestEventOne>(endpoint => endpoint
                    .ProduceTo(DefaultTopicName)
                    .SetKafkaKey(message => message?.ContentEventOne)
                    .EnableChunking(10)))));

        using Activity parentActivity = new("TestActivity");
        parentActivity.Start();

        await ProduceAsync(ProduceMethod.ProduceWithCallbacks, new TestEventOne { ContentEventOne = "one" });

        int chunksCount = DefaultTopic.GetAllMessages().Count;
        chunksCount.ShouldBeGreaterThan(1);

        // The activity is stopped only once, after all chunks have been acknowledged
        await AsyncTestingUtil.WaitAsync(() => logScopesCollector.Scopes.Count >= chunksCount && stoppedActivities.Count >= 1);
        ActivityTraceId traceId = parentActivity.TraceId;
        Activity produceActivity = stoppedActivities.Where(activity => activity.TraceId == traceId).ShouldHaveSingleItem();
        produceActivity.GetTagItem("messaging.kafka.message_key").ShouldBe("one");

        logScopesCollector.Scopes.Count.ShouldBe(chunksCount);
        logScopesCollector.Scopes.ShouldAllBe(scope =>
            (string?)scope["SpanId"] == produceActivity.SpanId.ToHexString() &&
            (string?)scope["messaging.kafka.message_key"] == "one");
        logScopesCollector.Scopes.Select(scope => scope["messaging.message_id"]).ShouldBe(
            Enumerable.Range(0, chunksCount).Select(offset => $"{DefaultTopicName}[0]@{offset}"),
            ignoreOrder: true);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldIncludeActivityTagsInProducedLogScope_WhenProducingFromOutbox()
    {
        TestingCollection<Activity> stoppedActivities = [];
        using ActivityListener listener = new();
        listener.ShouldListenTo = source => source.Name == "Silverback.Integration.Produce";
        listener.Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
        listener.ActivityStopped = stoppedActivities.Add;
        ActivitySource.AddActivityListener(listener);

        LogScopesCollector logScopesCollector = new(IntegrationLogEvents.MessageProduced);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging(logging => logging.AddProvider(logScopesCollector).Configure(LogScopesCollector.TrackActivity))
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
                .AddInMemoryOutbox()
                .AddOutboxWorker(worker => worker
                    .ProcessOutbox(outbox => outbox.UseMemory())
                    .WithInterval(TimeSpan.FromMilliseconds(50))))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer.Produce<TestEventOne>(
                    "my-endpoint",
                    endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .SetKafkaKey(message => message?.ContentEventOne)
                        .StoreToOutbox(outbox => outbox.UseMemory())))));

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        using (Activity activity = new("TestActivity"))
        {
            activity.Start();
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
        }

        // The message is produced by the outbox worker, logging the outcome from the thread delivering the acknowledgment
        await AsyncTestingUtil.WaitAsync(() => logScopesCollector.Scopes.Any(scope => scope.ContainsKey("messaging.message_id")));

        IReadOnlyDictionary<string, object?> scope = logScopesCollector.Scopes.Single(scope => scope.ContainsKey("messaging.message_id"));
        scope["messaging.message_id"].ShouldBe($"{DefaultTopicName}[0]@0");
        scope["messaging.kafka.partition"].ShouldBe($"{DefaultTopicName}[0]");
        scope["messaging.kafka.message_key"].ShouldBe("one");
        logScopesCollector.Messages.ShouldContain(message => message.Contains("BrokerMessageId: [0]@0", StringComparison.Ordinal));

        await AsyncTestingUtil.WaitAsync(() => stoppedActivities.Any(activity => activity.SpanId.ToHexString() == (string?)scope["SpanId"]));
        Activity produceActivity = stoppedActivities.Single(activity => activity.SpanId.ToHexString() == (string?)scope["SpanId"]);
        produceActivity.GetTagItem("messaging.message_id").ShouldBe($"{DefaultTopicName}[0]@0");
        produceActivity.GetTagItem("messaging.kafka.message_key").ShouldBe("one");
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldIncludeActivityTagsInProducedLogScope_WhenBehaviorStartsNestedActivity()
    {
        TestingCollection<Activity> stoppedActivities = [];
        using ActivityListener listener = new();
        listener.ShouldListenTo = source => source.Name == "Silverback.Integration.Produce";
        listener.Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
        listener.ActivityStopped = stoppedActivities.Add;
        ActivitySource.AddActivityListener(listener);

        LogScopesCollector logScopesCollector = new(IntegrationLogEvents.MessageProduced);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging(logging => logging.AddProvider(logScopesCollector).Configure(LogScopesCollector.TrackActivity))
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer.Produce<TestEventOne>(endpoint => endpoint
                    .ProduceTo(DefaultTopicName)
                    .SetKafkaKey(message => message?.ContentEventOne))))
            .AddSingletonBrokerBehavior<NestedActivityProducerBehavior>());

        using Activity parentActivity = new("TestActivity");
        parentActivity.Start();

        await ProduceAsync(ProduceMethod.Produce, new TestEventOne { ContentEventOne = "one" });

        await AsyncTestingUtil.WaitAsync(() => logScopesCollector.Scopes.Count >= 1 && stoppedActivities.Count >= 1);

        ActivityTraceId traceId = parentActivity.TraceId;
        Activity produceActivity = stoppedActivities.Where(activity => activity.TraceId == traceId).ShouldHaveSingleItem();
        produceActivity.GetTagItem("messaging.message_id").ShouldBe($"{DefaultTopicName}[0]@0");
        produceActivity.GetTagItem("messaging.kafka.message_key").ShouldBe("one");

        IReadOnlyDictionary<string, object?> scope = logScopesCollector.Scopes.ShouldHaveSingleItem();
        scope["SpanId"].ShouldBe(produceActivity.SpanId.ToHexString());
        scope["messaging.message_id"].ShouldBe($"{DefaultTopicName}[0]@0");
        scope["messaging.kafka.message_key"].ShouldBe("one");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProducerEndpoint_ShouldLogBrokerMessageIdWithoutTaggingCallerActivity_WhenRawProducing(bool produceAsync)
    {
        LogScopesCollector logScopesCollector = new(IntegrationLogEvents.MessageProduced);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging(logging => logging.AddProvider(logScopesCollector).Configure(LogScopesCollector.TrackActivity))
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer.Produce<TestEventOne>(endpoint => endpoint
                    .ProduceTo(DefaultTopicName)))));

        using Activity parentActivity = new("TestActivity");
        parentActivity.Start();

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        MessageHeader[] headers = [new(KafkaMessageHeaders.MessageKey, "one")];

        if (produceAsync)
            await producer.RawProduceAsync([0x01], headers);
        else
            producer.RawProduce([0x01], headers);

        logScopesCollector.Messages.ShouldHaveSingleItem().ShouldContain("BrokerMessageId: [0]@0");
        Activity.Current.ShouldBe(parentActivity);
        parentActivity.GetTagItem("messaging.kafka.message_key").ShouldBeNull();
        parentActivity.GetTagItem("messaging.message_id").ShouldBeNull();
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldNotFlowExecutionContextIntoMockedDeliveryCallbacks()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer.Produce<TestEventOne>(endpoint => endpoint
                    .ProduceTo(DefaultTopicName)))));

        // Like the real client, which invokes the delivery handler from its poll thread, the mock doesn't flow the caller's context
        AsyncLocal<string?> asyncLocal = new() { Value = "caller" };
        TaskCompletionSource<string?> valueInCallback = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Helper.GetProducerForEndpoint(DefaultTopicName).RawProduce(
            [0x01],
            null,
            _ => valueInCallback.SetResult(asyncLocal.Value),
            valueInCallback.SetException);

        (await valueInCallback.Task).ShouldBeNull();
    }

    // Not an async method, so that the changes to the current activity made by the synchronous produce methods flow back to the test
    private Task ProduceAsync(ProduceMethod produceMethod, TestEventOne message)
    {
        switch (produceMethod)
        {
            case ProduceMethod.PublishAsync:
                return Host.ServiceProvider.GetRequiredService<IPublisher>().PublishEventAsync(message);
            case ProduceMethod.Produce:
                Helper.GetProducerForEndpoint(DefaultTopicName).Produce(message);
                return Task.CompletedTask;
            case ProduceMethod.ProduceWithCallbacks:
                TaskCompletionSource<bool> produced = new(TaskCreationOptions.RunContinuationsAsynchronously);
                Helper.GetProducerForEndpoint(DefaultTopicName).Produce(
                    message,
                    null,
                    _ => produced.SetResult(true),
                    produced.SetException);
                return produced.Task;
            default:
                throw new ArgumentOutOfRangeException(nameof(produceMethod), produceMethod, null);
        }
    }

    // Starts another activity around the rest of the pipeline, so that the produce activity isn't the current one when producing
    private sealed class NestedActivityProducerBehavior : IProducerBehavior
    {
        public int SortIndex => BrokerBehaviorsSortIndexes.Producer.Activity + 1;

        public async ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next, CancellationToken cancellationToken)
        {
            using Activity nestedActivity = new("NestedActivity");
            nestedActivity.Start();

            await next(context, cancellationToken);
        }
    }
}
