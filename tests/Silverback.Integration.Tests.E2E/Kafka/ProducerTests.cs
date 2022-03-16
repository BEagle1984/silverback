// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class ProducerTests : KafkaTestFixture
{
    public ProducerTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Produce_Message_Produced()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Skip())))
                    .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.Broker.GetProducer(DefaultTopicName);

        producer.Produce(message);
        producer.Produce(message);
        producer.Produce(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);

        foreach (IInboundEnvelope envelope in Helper.Spy.InboundEnvelopes)
        {
            envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
        }
    }

    [Fact]
    public async Task Produce_MessageWithHeaders_Produced()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Skip())))
                    .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.Broker.GetProducer(DefaultTopicName);

        producer.Produce(message, new MessageHeaderCollection { { "one", "1" }, { "two", "2" } });
        producer.Produce(message, new MessageHeaderCollection { { "one", "1" }, { "two", "2" } });
        producer.Produce(message, new MessageHeaderCollection { { "one", "1" }, { "two", "2" } });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);

        foreach (IInboundEnvelope envelope in Helper.Spy.InboundEnvelopes)
        {
            envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("one", "1"));
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("two", "2"));
        }
    }

    [Fact]
    public async Task Produce_Envelope_Produced()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Skip())))
                    .AddIntegrationSpyAndSubscriber());

        IOutboundEnvelopeFactory envelopeFactory = Host.ServiceProvider.GetRequiredService<IOutboundEnvelopeFactory>();
        KafkaProducer producer = (KafkaProducer)Helper.Broker.GetProducer(DefaultTopicName);

        producer.Produce(
            envelopeFactory.CreateEnvelope(
                message,
                new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
                new KafkaProducerEndpoint(DefaultTopicName, Partition.Any, producer.Configuration)));
        producer.Produce(
            envelopeFactory.CreateEnvelope(
                message,
                new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
                new KafkaProducerEndpoint(DefaultTopicName, Partition.Any, producer.Configuration)));
        producer.Produce(
            envelopeFactory.CreateEnvelope(
                message,
                new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
                new KafkaProducerEndpoint(DefaultTopicName, Partition.Any, producer.Configuration)));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);

        foreach (IInboundEnvelope envelope in Helper.Spy.InboundEnvelopes)
        {
            envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("one", "1"));
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("two", "2"));
        }
    }

    [Fact]
    public async Task Produce_MessageUsingCallbacks_Produced()
    {
        int produced = 0;
        int errors = 0;

        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Skip())))
                    .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.Broker.GetProducer(DefaultTopicName);

        producer.Produce(
            message,
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        producer.Produce(
            message,
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        producer.Produce(
            message,
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));

        produced.Should().BeLessThan(3);

        await AsyncTestingUtil.WaitAsync(() => produced == 3);

        produced.Should().Be(3);
        errors.Should().Be(0);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        foreach (IInboundEnvelope envelope in Helper.Spy.InboundEnvelopes)
        {
            envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("one", "1"));
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("two", "2"));
        }
    }

    [Fact]
    public async Task Produce_EnvelopeUsingCallbacks_Produced()
    {
        int produced = 0;
        int errors = 0;

        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Skip())))
                    .AddIntegrationSpyAndSubscriber());

        IOutboundEnvelopeFactory envelopeFactory = Host.ServiceProvider.GetRequiredService<IOutboundEnvelopeFactory>();
        KafkaProducer producer = (KafkaProducer)Helper.Broker.GetProducer(DefaultTopicName);

        producer.Produce(
            envelopeFactory.CreateEnvelope(
                message,
                new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
                new KafkaProducerEndpoint(DefaultTopicName, Partition.Any, producer.Configuration)),
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        producer.Produce(
            envelopeFactory.CreateEnvelope(
                message,
                new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
                new KafkaProducerEndpoint(DefaultTopicName, Partition.Any, producer.Configuration)),
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        producer.Produce(
            envelopeFactory.CreateEnvelope(
                message,
                new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
                new KafkaProducerEndpoint(DefaultTopicName, Partition.Any, producer.Configuration)),
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));

        await AsyncTestingUtil.WaitAsync(() => produced == 3);

        produced.Should().Be(3);
        errors.Should().Be(0);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        foreach (IInboundEnvelope envelope in Helper.Spy.InboundEnvelopes)
        {
            envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
            envelope.Headers.Should()
                .ContainEquivalentOf(new MessageHeader("one", "1"));
            envelope.Headers.Should()
                .ContainEquivalentOf(new MessageHeader("two", "2"));
        }
    }

    [Fact]
    public async Task RawProduce_ByteArray_Produced()
    {
        byte[] rawMessage = { 0x01, 0x02, 0x03, 0x04, 0x05 };

        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Skip())))
                    .AddIntegrationSpyAndSubscriber());

        KafkaProducer producer = (KafkaProducer)Helper.Broker.GetProducer(DefaultTopicName);

        producer.RawProduce(
            rawMessage,
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } });
        producer.RawProduce(
            new KafkaProducerEndpoint(DefaultTopicName, Partition.Any, producer.Configuration),
            rawMessage,
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } });
        producer.RawProduce(
            rawMessage,
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawInboundEnvelopes.Should().HaveCount(3);

        foreach (IRawInboundEnvelope envelope in Helper.Spy.RawInboundEnvelopes)
        {
            envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("one", "1"));
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("two", "2"));
        }
    }

    [Fact]
    public async Task RawProduce_Stream_Produced()
    {
        byte[] rawMessage = BytesUtil.GetRandomBytes();

        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Skip())))
                    .AddIntegrationSpyAndSubscriber());

        KafkaProducer producer = (KafkaProducer)Helper.Broker.GetProducer(DefaultTopicName);

        producer.RawProduce(
            new MemoryStream(rawMessage),
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } });
        producer.RawProduce(
            new KafkaProducerEndpoint(DefaultTopicName, Partition.Any, producer.Configuration),
            new MemoryStream(rawMessage),
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } });
        producer.RawProduce(
            new MemoryStream(rawMessage),
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawInboundEnvelopes.Should().HaveCount(3);

        foreach (IRawInboundEnvelope envelope in Helper.Spy.RawInboundEnvelopes)
        {
            envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("one", "1"));
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("two", "2"));
        }
    }

    [Fact]
    public async Task RawProduce_ByteArrayUsingCallbacks_Produced()
    {
        int produced = 0;
        int errors = 0;

        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Skip())))
                    .AddIntegrationSpyAndSubscriber());

        KafkaProducer producer = (KafkaProducer)Helper.Broker.GetProducer(DefaultTopicName);

        producer.RawProduce(
            rawMessage,
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        producer.RawProduce(
            new KafkaProducerEndpoint(DefaultTopicName, Partition.Any, producer.Configuration),
            rawMessage,
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        producer.RawProduce(
            rawMessage,
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));

        produced.Should().BeLessThan(3);

        await AsyncTestingUtil.WaitAsync(() => produced == 3);

        produced.Should().Be(3);
        errors.Should().Be(0);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawInboundEnvelopes.Should().HaveCount(3);

        foreach (IRawInboundEnvelope envelope in Helper.Spy.RawInboundEnvelopes)
        {
            envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("one", "1"));
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("two", "2"));
        }
    }

    [Fact]
    public async Task RawProduce_StreamUsingCallbacks_Produced()
    {
        int produced = 0;
        int errors = 0;

        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Skip())))
                    .AddIntegrationSpyAndSubscriber());

        KafkaProducer producer = (KafkaProducer)Helper.Broker.GetProducer(DefaultTopicName);

        producer.RawProduce(
            new MemoryStream(rawMessage),
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        producer.RawProduce(
            new KafkaProducerEndpoint(DefaultTopicName, Partition.Any, producer.Configuration),
            new MemoryStream(rawMessage),
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        producer.RawProduce(
            new MemoryStream(rawMessage),
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));

        produced.Should().BeLessThan(3);

        await AsyncTestingUtil.WaitAsync(() => produced == 3);

        produced.Should().Be(3);
        errors.Should().Be(0);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawInboundEnvelopes.Should().HaveCount(3);

        foreach (IRawInboundEnvelope envelope in Helper.Spy.RawInboundEnvelopes)
        {
            envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("one", "1"));
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("two", "2"));
        }
    }

    [Fact]
    public async Task ProduceAsync_Message_Produced()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Skip())))
                    .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.Broker.GetProducer(DefaultTopicName);

        await producer.ProduceAsync(message);
        await producer.ProduceAsync(message);
        await producer.ProduceAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);

        foreach (IInboundEnvelope envelope in Helper.Spy.InboundEnvelopes)
        {
            envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
        }
    }

    [Fact]
    public async Task ProduceAsync_MessageWithHeaders_ProducedAndConsumed()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Skip())))
                    .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.Broker.GetProducer(DefaultTopicName);

        await producer.ProduceAsync(
            message,
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } });
        await producer.ProduceAsync(
            message,
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } });
        await producer.ProduceAsync(
            message,
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);

        foreach (IInboundEnvelope envelope in Helper.Spy.InboundEnvelopes)
        {
            envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("one", "1"));
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("two", "2"));
        }
    }

    [Fact]
    public async Task ProduceAsync_Envelope_ProducedAndConsumed()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Skip())))
                    .AddIntegrationSpyAndSubscriber());

        IOutboundEnvelopeFactory envelopeFactory = Host.ServiceProvider.GetRequiredService<IOutboundEnvelopeFactory>();
        KafkaProducer producer = (KafkaProducer)Helper.Broker.GetProducer(DefaultTopicName);

        await producer.ProduceAsync(
            envelopeFactory.CreateEnvelope(
                message,
                new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
                new KafkaProducerEndpoint(DefaultTopicName, Partition.Any, producer.Configuration)));
        await producer.ProduceAsync(
            envelopeFactory.CreateEnvelope(
                message,
                new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
                new KafkaProducerEndpoint(DefaultTopicName, Partition.Any, producer.Configuration)));
        await producer.ProduceAsync(
            envelopeFactory.CreateEnvelope(
                message,
                new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
                new KafkaProducerEndpoint(DefaultTopicName, Partition.Any, producer.Configuration)));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);

        foreach (IInboundEnvelope envelope in Helper.Spy.InboundEnvelopes)
        {
            envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("one", "1"));
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("two", "2"));
        }
    }

    [Fact]
    public async Task ProduceAsync_MessageUsingCallbacks_ProducedAndConsumed()
    {
        int produced = 0;
        int errors = 0;

        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Skip())))
                    .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.Broker.GetProducer(DefaultTopicName);

        await producer.ProduceAsync(
            message,
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        await producer.ProduceAsync(
            message,
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        await producer.ProduceAsync(
            message,
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));

        produced.Should().BeLessThan(3);

        await AsyncTestingUtil.WaitAsync(() => produced == 3);

        produced.Should().Be(3);
        errors.Should().Be(0);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        foreach (IInboundEnvelope envelope in Helper.Spy.InboundEnvelopes)
        {
            envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
            envelope.Headers.Should()
                .ContainEquivalentOf(new MessageHeader("one", "1"));
            envelope.Headers.Should()
                .ContainEquivalentOf(new MessageHeader("two", "2"));
        }
    }

    [Fact]
    public async Task RawProduceAsync_ByteArray_ProducedAndConsumed()
    {
        byte[] rawMessage = BytesUtil.GetRandomBytes();

        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Skip())))
                    .AddIntegrationSpyAndSubscriber());

        KafkaProducer producer = (KafkaProducer)Helper.Broker.GetProducer(DefaultTopicName);

        await producer.RawProduceAsync(
            rawMessage,
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } });
        await producer.RawProduceAsync(
            new KafkaProducerEndpoint(DefaultTopicName, Partition.Any, producer.Configuration),
            rawMessage,
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } });
        await producer.RawProduceAsync(
            rawMessage,
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawInboundEnvelopes.Should().HaveCount(3);

        foreach (IRawInboundEnvelope envelope in Helper.Spy.RawInboundEnvelopes)
        {
            envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("one", "1"));
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("two", "2"));
        }
    }

    [Fact]
    public async Task RawProduceAsync_Stream_ProducedAndConsumed()
    {
        byte[] rawMessage = BytesUtil.GetRandomBytes();

        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Skip())))
                    .AddIntegrationSpyAndSubscriber());

        KafkaProducer producer = (KafkaProducer)Helper.Broker.GetProducer(DefaultTopicName);

        await producer.RawProduceAsync(
            new MemoryStream(rawMessage),
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } });
        await producer.RawProduceAsync(
            new KafkaProducerEndpoint(DefaultTopicName, Partition.Any, producer.Configuration),
            new MemoryStream(rawMessage),
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } });
        await producer.RawProduceAsync(
            new MemoryStream(rawMessage),
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawInboundEnvelopes.Should().HaveCount(3);

        foreach (IRawInboundEnvelope envelope in Helper.Spy.RawInboundEnvelopes)
        {
            envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("one", "1"));
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("two", "2"));
        }
    }

    [Fact]
    public async Task RawProduceAsync_ByteArrayUsingCallbacks_ProducedAndConsumed()
    {
        int produced = 0;
        int errors = 0;

        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Skip())))
                    .AddIntegrationSpyAndSubscriber());

        KafkaProducer producer = (KafkaProducer)Helper.Broker.GetProducer(DefaultTopicName);

        await producer.RawProduceAsync(
            rawMessage,
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        await producer.RawProduceAsync(
            new KafkaProducerEndpoint(DefaultTopicName, Partition.Any, producer.Configuration),
            rawMessage,
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        await producer.RawProduceAsync(
            rawMessage,
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));

        produced.Should().BeLessThan(3);

        await AsyncTestingUtil.WaitAsync(() => produced == 3);

        produced.Should().Be(3);
        errors.Should().Be(0);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawInboundEnvelopes.Should().HaveCount(3);

        foreach (IRawInboundEnvelope envelope in Helper.Spy.RawInboundEnvelopes)
        {
            envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
            envelope.Headers.Should()
                .ContainEquivalentOf(new MessageHeader("one", "1"));
            envelope.Headers.Should()
                .ContainEquivalentOf(new MessageHeader("two", "2"));
        }
    }

    [Fact]
    public async Task RawProduceAsync_StreamUsingCallbacks_ProducedAndConsumed()
    {
        int produced = 0;
        int errors = 0;

        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Skip())))
                    .AddIntegrationSpyAndSubscriber());

        KafkaProducer producer = (KafkaProducer)Helper.Broker.GetProducer(DefaultTopicName);

        await producer.RawProduceAsync(
            new MemoryStream(rawMessage),
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        await producer.RawProduceAsync(
            new KafkaProducerEndpoint(DefaultTopicName, Partition.Any, producer.Configuration),
            new MemoryStream(rawMessage),
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        await producer.RawProduceAsync(
            new MemoryStream(rawMessage),
            new MessageHeaderCollection { { "one", "1" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));

        produced.Should().BeLessThan(3);

        await AsyncTestingUtil.WaitAsync(() => produced == 3);

        produced.Should().Be(3);
        errors.Should().Be(0);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        foreach (IInboundEnvelope envelope in Helper.Spy.InboundEnvelopes)
        {
            envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("one", "1"));
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("two", "2"));
        }
    }

    [Fact]
    public async Task RawProduce_WithMessageIdHeader_KafkaKeySet()
    {
        int produced = 0;
        int errors = 0;

        TestEventOne message = new() { Content = "Hello E2E!" };
        MessageHeaderCollection headers = new() { { DefaultMessageHeaders.MessageId, "42-42-42" } };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message, headers);

        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Skip())))
                    .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.Broker.GetProducer(DefaultTopicName);

        for (int i = 0; i < 5; i++)
        {
            await producer.RawProduceAsync(
                rawMessage,
                headers,
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));
        }

        produced.Should().BeLessThan(3);

        await AsyncTestingUtil.WaitAsync(() => produced == 5);

        produced.Should().Be(5);
        errors.Should().Be(0);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawInboundEnvelopes.Should().HaveCount(5);

        foreach (IRawInboundEnvelope envelope in Helper.Spy.RawInboundEnvelopes)
        {
            envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader(KafkaMessageHeaders.KafkaMessageKey, "42-42-42"));
        }
    }

    [Fact]
    public async Task RawProduce_WithKafkaKeyHeader_KafkaKeySet()
    {
        int produced = 0;
        int errors = 0;

        TestEventOne message = new() { Content = "Hello E2E!" };
        MessageHeaderCollection headers = new() { { DefaultMessageHeaders.MessageId, "42-42-42" } };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message, headers);

        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Skip())))
                    .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.Broker.GetProducer(DefaultTopicName);

        for (int i = 0; i < 5; i++)
        {
            await producer.RawProduceAsync(
                rawMessage,
                headers,
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));
        }

        produced.Should().BeLessThan(3);

        await AsyncTestingUtil.WaitAsync(() => produced == 5);

        produced.Should().Be(5);
        errors.Should().Be(0);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawInboundEnvelopes.Should().HaveCount(5);

        foreach (IRawInboundEnvelope envelope in Helper.Spy.RawInboundEnvelopes)
        {
            envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader(KafkaMessageHeaders.KafkaMessageKey, "42-42-42"));
        }
    }

    [Fact]
    public async Task RawProduce_ByDefault_TimestampKeySet()
    {
        int produced = 0;
        int errors = 0;

        TestEventOne message = new()
        {
            Content = "Hello E2E!"
        };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Skip())))
                    .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.Broker.GetProducer(DefaultTopicName);

        for (int i = 0; i < 5; i++)
        {
            await producer.RawProduceAsync(
                rawMessage,
                new MessageHeaderCollection(),
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));
        }

        produced.Should().BeLessThan(3);

        await AsyncTestingUtil.WaitAsync(() => produced == 5);

        produced.Should().Be(5);
        errors.Should().Be(0);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawInboundEnvelopes.Should().HaveCount(5);

        foreach (IRawInboundEnvelope envelope in Helper.Spy.RawInboundEnvelopes)
        {
            envelope.RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
            envelope.Headers.Contains(KafkaMessageHeaders.TimestampKey).Should().BeTrue();
        }
    }
}
