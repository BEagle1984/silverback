// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Confluent.Kafka;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Tests.Extended.Benchmarks.Common;

namespace Silverback.Tests.Extended.Benchmarks.Micro;

/// <summary>
///     Measures the serialization of a dynamic Kafka endpoint to (and from) the header value stored in the outbox.
/// </summary>
[Config(typeof(MicroBenchmarkConfig))]
[MemoryDiagnoser]
public class KafkaDynamicEndpointBenchmark
{
    private Func<KafkaProducerEndpoint, string> _serialize = _ => "";

    private Func<string, KafkaProducerEndpointConfiguration, KafkaProducerEndpoint> _deserialize = (_, _) => null!;

    private KafkaProducerEndpoint _endpoint = null!;

    private KafkaProducerEndpointConfiguration _configuration = null!;

    private string _serializedEndpoint = "";

    [GlobalSetup]
    public void Setup()
    {
        KafkaDynamicProducerEndpointResolver<BenchmarkMessage> resolver = new(GetTopicPartition);

        // The (de)serialization methods are protected: they are invoked by the resolver when storing and reading the outbox messages
        const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        _serialize = typeof(KafkaDynamicProducerEndpointResolver<BenchmarkMessage>)
            .GetMethod("SerializeEndpoint", bindingFlags)!
            .CreateDelegate<Func<KafkaProducerEndpoint, string>>(resolver);
        _deserialize = typeof(KafkaDynamicProducerEndpointResolver<BenchmarkMessage>)
            .GetMethod("DeserializeEndpoint", bindingFlags)!
            .CreateDelegate<Func<string, KafkaProducerEndpointConfiguration, KafkaProducerEndpoint>>(resolver);

        _configuration = new KafkaProducerEndpointConfiguration();
        _endpoint = new KafkaProducerEndpoint(new TopicPartition("benchmark-topic", 3), _configuration);
        _serializedEndpoint = _serialize(_endpoint);
    }

    [Benchmark]
    public string Serialize() => _serialize(_endpoint);

    [Benchmark]
    public KafkaProducerEndpoint Deserialize() => _deserialize(_serializedEndpoint, _configuration);

    private static TopicPartition GetTopicPartition(BenchmarkMessage? message) => new("benchmark-topic", 3);
}
