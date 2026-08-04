// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class NullMessageHandlingTests : KafkaTests
{
    public NullMessageHandlingTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task NullMessage_ShouldConsumeTombstone()
    {
        Tombstone? tombstone = null;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddDelegateSubscriber<Tombstone>(Handle)
            .AddIntegrationSpy());

        void Handle(Tombstone message) => tombstone = message;

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.RawProduceAsync(
            (byte[]?)null,
            new MessageHeaderCollection
            {
                { KafkaMessageHeaders.MessageKey, "42" }
            });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        tombstone.ShouldBeOfType<Tombstone>();
        tombstone!.MessageKey.ShouldBe("42");
    }

    [Fact]
    public async Task NonNullMessage_ShouldNotConsumeTombstone()
    {
        Tombstone? tombstone = null;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddDelegateSubscriber<Tombstone>(Handle)
            .AddIntegrationSpy());

        void Handle(Tombstone message) => tombstone = message;

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(
            new TestEventOne(),
            new MessageHeaderCollection
            {
                { KafkaMessageHeaders.MessageKey, "42" }
            });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        tombstone.ShouldBeNull();
    }

    [Fact]
    public async Task NullMessage_ShouldConsumeTypedTombstone_WhenMessageTypeHeaderIsSet()
    {
        Tombstone? tombstone = null;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddDelegateSubscriber<Tombstone<TestEventOne>>(Handle)
            .AddIntegrationSpy());

        void Handle(Tombstone<TestEventOne> message) => tombstone = message;

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.RawProduceAsync(
            (byte[]?)null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName },
                { KafkaMessageHeaders.MessageKey, "42" }
            });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        tombstone.ShouldBeOfType<Tombstone<TestEventOne>>();
        tombstone!.MessageKey.ShouldBe("42");
    }

    [Fact]
    public async Task NullMessage_ShouldConsumeTypedTombstone_WhenConsumingSpecificType()
    {
        Tombstone? tombstone = null;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddDelegateSubscriber<Tombstone<TestEventOne>>(Handle)
            .AddIntegrationSpy());

        void Handle(Tombstone<TestEventOne> message) => tombstone = message;

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.RawProduceAsync(
            (byte[]?)null,
            new MessageHeaderCollection
            {
                { KafkaMessageHeaders.MessageKey, "42" }
            });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        tombstone.ShouldBeOfType<Tombstone<TestEventOne>>();
        tombstone!.MessageKey.ShouldBe("42");
    }

    [Fact]
    public async Task NullMessage_ShouldNotBeConsumed()
    {
        bool consumed = false;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddDelegateSubscriber<TestEventOne>(Handle)
            .AddIntegrationSpy());

        void Handle(TestEventOne message) => consumed = true;

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.RawProduceAsync(
            (byte[]?)null,
            new MessageHeaderCollection
            {
                { KafkaMessageHeaders.MessageKey, "42" }
            });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        consumed.ShouldBeFalse();
    }

    [Fact]
    public async Task NullMessage_ShouldConsumeInboundEnvelope()
    {
        IInboundEnvelope<TestEventOne>? consumedEnvelope = null;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddDelegateSubscriber<IInboundEnvelope<TestEventOne>>(Handle)
            .AddIntegrationSpy());

        void Handle(IInboundEnvelope<TestEventOne> envelope) => consumedEnvelope = envelope;

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.RawProduceAsync(
            (byte[]?)null,
            new MessageHeaderCollection
            {
                { KafkaMessageHeaders.MessageKey, "42" }
            });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        consumedEnvelope.ShouldNotBeNull();
        consumedEnvelope!.GetKafkaKey().ShouldBe("42");
        consumedEnvelope!.Message.ShouldBeNull();
    }

    /// <summary>
    ///     Regression test for SubscribedMethodsCache poisoning by tombstone-first messages.
    ///
    ///     When a compacted/state topic is consumed from the earliest offset, tombstone records often
    ///     appear before the corresponding regular records. The cache key is the envelope CLR type
    ///     (<c>InboundEnvelope&lt;T&gt;</c>), but <c>AreCompatible</c> is instance-dependent (it branches on
    ///     <c>envelope.Message != null</c>). If a tombstone is seen first, only the tombstone handler is
    ///     stored in the cache; subsequent regular messages reuse that stale entry and their typed subscriber
    ///     is never invoked, which causes an <see cref="UnhandledMessageException" /> and stops the consumer.
    /// </summary>
    [Fact]
    public async Task TombstoneFirst_ThenRegularMessage_ShouldInvokeBothSubscribers()
    {
        int received = 0;
        int tombstonesReceived = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddDelegateSubscriber<TestEventOne>(_ => received++)
            .AddDelegateSubscriber<Tombstone<TestEventOne>>(_ => tombstonesReceived++)
            .AddIntegrationSpy());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        // Tombstone arrives first — simulates reading a compacted state topic from the earliest offset
        // where a key was previously deleted. This is the scenario that poisons the cache on buggy code.
        await producer.RawProduceAsync(
            (byte[]?)null,
            new MessageHeaderCollection { { KafkaMessageHeaders.MessageKey, "key-1" } });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        tombstonesReceived.ShouldBe(1);

        // A regular (non-tombstone) message for the same type now arrives.
        // On buggy code: the cache is poisoned — only the Tombstone handler is in the cached set,
        // TombstoneMessageArgumentResolver.GetValue returns null (not a tombstone), SkipInvocationIfNull
        // skips the invocation, no handler runs, ThrowIfUnhandled fires and stops the consumer.
        // On fixed code: the cache correctly resolves both handlers per-message-instance.
        await producer.ProduceAsync(
            new TestEventOne(),
            new MessageHeaderCollection { { KafkaMessageHeaders.MessageKey, "key-1" } });

        // Use a short timeout so the test fails fast on buggy code (consumer stops → offset never
        // committed → the default 30 s wait would time out). throwTimeoutException:false lets us
        // fall through to the assertions which produce a clean failure.
        await Helper.WaitUntilAllMessagesAreConsumedAsync(throwTimeoutException: false, TimeSpan.FromSeconds(3));

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
        tombstonesReceived.ShouldBe(1);
        received.ShouldBe(1);
    }
}
