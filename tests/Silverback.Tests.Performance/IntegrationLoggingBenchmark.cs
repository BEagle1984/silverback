// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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

    private readonly IInboundLogger<IntegrationLoggingBenchmark> _inboundLogger;

    private readonly IOutboundLogger<IntegrationLoggingBenchmark> _outboundLogger;

    public IntegrationLoggingBenchmark()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(broker => broker.AddKafka()));

        _inboundLogger =
            serviceProvider.GetRequiredService<IInboundLogger<IntegrationLoggingBenchmark>>();

        _outboundLogger =
            serviceProvider.GetRequiredService<IOutboundLogger<IntegrationLoggingBenchmark>>();

        KafkaConsumerEndpoint consumerEndpoint = new(
            "test-topic",
            42,
            new KafkaConsumerConfigurationBuilder<TestEventOne>()
                .ConfigureClient(
                    configuration => configuration
                        .WithBootstrapServers("PLAINTEXT://performance:9092")
                        .WithClientId("test-client"))
                .ConsumeFrom("test-topic")
                .Build());

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
            new KafkaOffset("test", 4, 2));

        KafkaProducerEndpoint producerEndpoint = new(
            "test-topic",
            Partition.Any,
            new KafkaProducerConfigurationBuilder<TestEventOne>()
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://performance:9092"))
                .ProduceTo("test-topic")
                .Build());

        _outboundEnvelope =
            new OutboundEnvelope(
                new TestEventOne(),
                new MessageHeaderCollection
                {
                    new("Test", "Test")
                },
                producerEndpoint,
                false,
                new KafkaOffset("test", 4, 2));
    }

    [Benchmark]
    public void LogInbound()
    {
        _inboundLogger.LogProcessing(_inboundEnvelope);
    }

    [Benchmark]
    public void LogOutbound()
    {
        _outboundLogger.LogProduced(_outboundEnvelope);
    }

    [Benchmark]
    public void LogDisabled()
    {
        _outboundLogger.LogWrittenToOutbox(_outboundEnvelope);
    }
}
