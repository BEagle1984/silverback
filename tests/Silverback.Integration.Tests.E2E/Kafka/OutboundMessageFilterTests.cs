// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Filter;
using Silverback.Messaging.Producing.Routing;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class OutboundMessageFilterTests : KafkaTests
{
    private static readonly TimeSpan PublishTimeout = TimeSpan.FromSeconds(10);

    public OutboundMessageFilterTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Filter_ShouldFilterOutboundMessages()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .Filter(eventOne => eventOne?.ContentEventOne != "discard")))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne());
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "discard" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "something" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Filter_ShouldFilterOutboundEnvelopes()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .Filter(envelope => envelope.Message?.ContentEventOne != "discard")))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne());
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "discard" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "something" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Filter_ShouldFilterOutboundMessagesUsingFilterInstance()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .Filter(new DiscardFilter())))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "discard" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "two" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.InboundEnvelopes.Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .ShouldBe(["one", "two"], true);
    }

    [Fact]
    public async Task Filter_ShouldFilterOutboundMessagesUsingFilterResolvedFromPublisherScope()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddScoped<DiscardSettings>()
            .AddScoped<ScopedDiscardFilter>()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .Filter<ScopedDiscardFilter>()))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<DiscardSettings>().ContentToDiscard = "one";
            IPublisher publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "two" });
        }

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<DiscardSettings>().ContentToDiscard = "two";
            IPublisher publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "two" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(4);
        Helper.Spy.InboundEnvelopes.Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .ShouldBe(["two", "one"], true);
    }

    [Fact]
    public async Task Filter_ShouldFilterMessagesProducedViaProducerUsingFilterResolvedFromServiceProvider()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSingleton<DiscardFilter>()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .Filter<DiscardFilter>()))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "one" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "discard" });
        producer.Produce(new TestEventOne { ContentEventOne = "two" });
        producer.Produce(new TestEventOne { ContentEventOne = "discard" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(4);
        Helper.Spy.InboundEnvelopes.Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .ShouldBe(["one", "two"], true);
    }

    [Fact]
    public async Task Filter_ShouldResolveFilterFromEnvelopeContextScope_WhenProducingEnvelopeViaProducer()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddScoped<DiscardSettings>()
            .AddScoped<ScopedDiscardFilter>()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .Filter<ScopedDiscardFilter>()))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<DiscardSettings>().ContentToDiscard = "discard";
            ISilverbackContext context = scope.ServiceProvider.GetRequiredService<ISilverbackContext>();

            IOutboundEnvelope CreateEnvelope(string content) =>
                OutboundEnvelopeFactory.CreateEnvelope(
                    new TestEventOne { ContentEventOne = content },
                    null,
                    producer.EndpointConfiguration,
                    producer,
                    context);

            producer.Produce(CreateEnvelope("one"));
            producer.Produce(CreateEnvelope("discard"));
            await producer.ProduceAsync(CreateEnvelope("two"));
            await producer.ProduceAsync(CreateEnvelope("discard"));
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(4);
        Helper.Spy.InboundEnvelopes.Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .ShouldBe(["one", "two"], true);
    }

    [Fact]
    public async Task Filter_ShouldInvokeAsyncFilter_WhenPublishingBatch()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSingleton<RecordingFilter>()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .Filter<RecordingFilter>()))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        static async IAsyncEnumerable<TestEventOne> GetMessagesAsync()
        {
            yield return new TestEventOne { ContentEventOne = "three" };
            await Task.Delay(10);
            yield return new TestEventOne { ContentEventOne = "four" };
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(
            new TestEventOne[]
            {
                new() { ContentEventOne = "one" },
                new() { ContentEventOne = "two" }
            });
        await publisher.PublishAsync(GetMessagesAsync());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Host.ServiceProvider.GetRequiredService<RecordingFilter>().Contents.ShouldBe(["one", "two", "three", "four"]);
        Helper.Spy.InboundEnvelopes.Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .ShouldBe(["one", "two", "three", "four"], true);
    }

    [Fact]
    public async Task Filter_ShouldFilterOutboundMessages_WhenPublishingEnumerable()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .Filter(eventOne => eventOne?.ContentEventOne != "discard"))))
            .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(
                new TestEventOne[]
                {
                    new() { ContentEventOne = "one" },
                    new() { ContentEventOne = "discard" },
                    new() { ContentEventOne = "two" },
                    new() { ContentEventOne = "discard" }
                }.AsEnumerable())
            .WaitAsync(PublishTimeout);

        Helper.Spy.RawOutboundEnvelopes.Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne).ShouldBe(["one", "two"]);
        DefaultTopic.MessagesCount.ShouldBe(2);
    }

    [Fact]
    public async Task Filter_ShouldFilterOutboundMessages_WhenPublishingAsyncEnumerable()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .Filter(eventOne => eventOne?.ContentEventOne != "discard"))))
            .AddIntegrationSpy());

        static async IAsyncEnumerable<TestEventOne> GetMessagesAsync()
        {
            yield return new TestEventOne { ContentEventOne = "discard" };
            await Task.Delay(10);
            yield return new TestEventOne { ContentEventOne = "one" };
            await Task.Delay(10);
            yield return new TestEventOne { ContentEventOne = "discard" };
            yield return new TestEventOne { ContentEventOne = "two" };
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(GetMessagesAsync()).WaitAsync(PublishTimeout);

        Helper.Spy.RawOutboundEnvelopes.Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne).ShouldBe(["one", "two"]);
        DefaultTopic.MessagesCount.ShouldBe(2);
    }

    private sealed class DiscardFilter : IOutboundMessageFilter
    {
        public async ValueTask<bool> ShouldProduceAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken)
        {
            // Yield without capturing the synchronization context, since the caller might be waiting synchronously
            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

            return envelope.Message is not TestEventOne { ContentEventOne: "discard" };
        }
    }

    private sealed class RecordingFilter : IOutboundMessageFilter
    {
        private readonly ConcurrentQueue<string?> _contents = new();

        public IReadOnlyCollection<string?> Contents => _contents;

        public async ValueTask<bool> ShouldProduceAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken)
        {
            // Yield without capturing the synchronization context, since the caller might be waiting synchronously
            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

            _contents.Enqueue((envelope.Message as TestEventOne)?.ContentEventOne);

            return true;
        }
    }

    private sealed class DiscardSettings
    {
        public string? ContentToDiscard { get; set; }
    }

    private sealed class ScopedDiscardFilter : IOutboundMessageFilter
    {
        private readonly DiscardSettings _settings;

        public ScopedDiscardFilter(DiscardSettings settings)
        {
            _settings = settings;
        }

        public async ValueTask<bool> ShouldProduceAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken)
        {
            await Task.Yield();

            return envelope.Message is TestEventOne message && message.ContentEventOne != _settings.ContentToDiscard;
        }
    }
}
