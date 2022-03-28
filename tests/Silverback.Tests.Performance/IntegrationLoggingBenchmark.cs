// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;

namespace Silverback.Tests.Performance;

// 3.0.0 - With legacy logging and custom log templates
//
// |                Method |       Mean |    Error |   StdDev |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
// |---------------------- |-----------:|---------:|---------:|-------:|-------:|------:|----------:|
// | LogErrorSingleMessage | 1,522.3 ns | 24.57 ns | 22.98 ns | 0.1221 | 0.0305 |     - |    1368 B |
// |      LogErrorSequence | 1,789.9 ns | 22.54 ns | 21.09 ns | 0.1831 | 0.0458 |     - |    1960 B |
// |       LogInfoDisabled |   589.3 ns | 11.35 ns | 20.47 ns | 0.0153 |      - |     - |     264 B |
//
// 3.0.0 - High-performance logging
//
// |      Method |      Mean |    Error |   StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
// |------------ |----------:|---------:|---------:|-------:|------:|------:|----------:|
// |  LogInbound | 480.21 ns | 3.524 ns | 2.751 ns | 0.0763 |     - |     - |     920 B |
// | LogOutbound | 380.83 ns | 4.014 ns | 3.352 ns | 0.0763 |     - |     - |     920 B |
// | LogDisabled |  94.21 ns | 1.104 ns | 0.922 ns |      - |     - |     - |     128 B |
[SimpleJob(invocationCount: 65536)]
[MemoryDiagnoser]
public class IntegrationLoggingBenchmark
{
    private readonly IRawInboundEnvelope _inboundEnvelope;

    private readonly OutboundEnvelope _outboundEnvelope;

    private readonly IConsumerLogger<IntegrationLoggingBenchmark> _consumerLogger;

    private readonly IProducerLogger<IntegrationLoggingBenchmark> _producerLogger;

    public IntegrationLoggingBenchmark()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(broker => broker.AddKafka()));

        _consumerLogger =
            serviceProvider.GetRequiredService<IConsumerLogger<IntegrationLoggingBenchmark>>();

        _producerLogger =
            serviceProvider.GetRequiredService<IProducerLogger<IntegrationLoggingBenchmark>>();

        KafkaConsumerEndpoint consumerEndpoint = new(
            "test-topic",
            42,
            new KafkaConsumerEndpointConfigurationBuilder<TestEventOne>().ConsumeFrom("test-topic").Build());

        _inboundEnvelope = new RawInboundEnvelope(
            Array.Empty<byte>(),
            new MessageHeaderCollection
            {
                new("Test", "Test"),
                new(DefaultMessageHeaders.FailedAttempts, "1"),
                new(DefaultMessageHeaders.MessageType, "Something.Xy"),
                new(DefaultMessageHeaders.MessageId, "1234"),
                new(KafkaMessageHeaders.KafkaMessageKey, "key1234")
            },
            consumerEndpoint,
            new TestConsumer(),
            new KafkaOffset(new TopicPartitionOffset("test", 4, 2)));

        KafkaProducerEndpoint producerEndpoint = new(
            "test-topic",
            Partition.Any,
            new KafkaProducerEndpointConfigurationBuilder<TestEventOne>().ProduceTo("test-topic").Build());

        _outboundEnvelope =
            new OutboundEnvelope(
                new TestEventOne(),
                new MessageHeaderCollection
                {
                    new("Test", "Test")
                },
                producerEndpoint,
                new TestProducer(),
                false,
                new KafkaOffset(new TopicPartitionOffset("test", 4, 2)));
    }

    [Benchmark]
    public void LogInbound()
    {
        _consumerLogger.LogProcessing(_inboundEnvelope);
    }

    [Benchmark]
    public void LogOutbound()
    {
        _producerLogger.LogProduced(_outboundEnvelope);
    }

    [Benchmark]
    public void LogDisabled()
    {
        _producerLogger.LogStoringIntoOutbox(_outboundEnvelope);
    }

    private class TestClient : IBrokerClient
    {
        public string Name => "client1-name";

        public string DisplayName => "client1";

        public AsyncEvent<BrokerClient> Initializing { get; } = new();

        public AsyncEvent<BrokerClient> Initialized { get; } = new();

        public AsyncEvent<BrokerClient> Disconnecting { get; } = new();

        public AsyncEvent<BrokerClient> Disconnected { get; } = new();

        public ClientStatus Status => ClientStatus.Initializing;

        public void Dispose() => throw new NotSupportedException();

        public ValueTask DisposeAsync() => throw new NotSupportedException();

        public ValueTask ConnectAsync() => throw new NotSupportedException();

        public ValueTask DisconnectAsync() => throw new NotSupportedException();

        public ValueTask ReconnectAsync() => throw new NotSupportedException();
    }

    private class TestConsumer : IConsumer
    {
        public string Name => "consumer1-name";

        public string DisplayName => "consumer1";

        public IBrokerClient Client { get; } = new TestClient();

        public IReadOnlyCollection<ConsumerEndpointConfiguration> EndpointsConfiguration { get; } = Array.Empty<ConsumerEndpointConfiguration>();

        public IConsumerStatusInfo StatusInfo { get; } = new ConsumerStatusInfo();

        public ValueTask TriggerReconnectAsync() => throw new NotSupportedException();

        public ValueTask StartAsync() => throw new NotSupportedException();

        public ValueTask StopAsync() => throw new NotSupportedException();

        public ValueTask CommitAsync(IBrokerMessageIdentifier brokerMessageIdentifier) => throw new NotSupportedException();

        public ValueTask CommitAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers) => throw new NotSupportedException();

        public ValueTask RollbackAsync(IBrokerMessageIdentifier brokerMessageIdentifier) => throw new NotSupportedException();

        public ValueTask RollbackAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers) => throw new NotSupportedException();

        public int IncrementFailedAttempts(IRawInboundEnvelope envelope) => throw new NotSupportedException();
    }

    private class TestProducer : IProducer
    {
        public string Name => "producer1-name";

        public string DisplayName => "producer1";

        public IBrokerClient Client { get; } = new TestClient();

        public ProducerEndpointConfiguration EndpointConfiguration { get; } = TestProducerEndpointConfiguration.GetDefault();

        public IBrokerMessageIdentifier Produce(object? message, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public IBrokerMessageIdentifier Produce(IOutboundEnvelope envelope) => throw new NotSupportedException();

        public void Produce(object? message, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public void Produce(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public IBrokerMessageIdentifier RawProduce(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public IBrokerMessageIdentifier RawProduce(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public IBrokerMessageIdentifier RawProduce(ProducerEndpoint endpoint, byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public IBrokerMessageIdentifier RawProduce(ProducerEndpoint endpoint, Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public void RawProduce(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public void RawProduce(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public void RawProduce(ProducerEndpoint endpoint, byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public void RawProduce(ProducerEndpoint endpoint, Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> ProduceAsync(object? message, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> ProduceAsync(IOutboundEnvelope envelope) => throw new NotSupportedException();

        public ValueTask ProduceAsync(object? message, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public ValueTask ProduceAsync(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(byte[]? message, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(Stream? message, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(ProducerEndpoint endpoint, byte[]? message, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(ProducerEndpoint endpoint, Stream? message, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public ValueTask RawProduceAsync(byte[]? message, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public ValueTask RawProduceAsync(Stream? message, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public ValueTask RawProduceAsync(ProducerEndpoint endpoint, byte[]? message, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public ValueTask RawProduceAsync(ProducerEndpoint endpoint, Stream? message, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();
    }
}
