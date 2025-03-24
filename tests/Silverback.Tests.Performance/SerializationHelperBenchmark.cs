// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using NSubstitute;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types.Domain;
using SerializationHelper = Silverback.Messaging.Serialization.SerializationHelper;

namespace Silverback.Tests.Performance;

/*
 * | Method                                 | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
 * |--------------------------------------- |----------:|---------:|---------:|------:|-------:|----------:|------------:|
 * | CreateTypedInboundEnvelopeViaActivator | 432.88 ns | 2.192 ns | 1.943 ns |  1.00 | 0.0587 |     616 B |        1.00 |
 * | CreateTypedInboundEnvelope             |  82.25 ns | 0.379 ns | 0.354 ns |  0.19 | 0.0213 |     224 B |        0.36 |
 */

[SimpleJob]
[MemoryDiagnoser]
public class SerializationHelperBenchmark
{
    private readonly IRawInboundEnvelope _rawInboundEnvelope;

    private readonly TestEventOne _testEventOne = new();

    public SerializationHelperBenchmark()
    {
        _rawInboundEnvelope = new RawInboundEnvelope(
            new byte[42],
            [],
            new KafkaConsumerEndpoint("topic", 0, new KafkaConsumerEndpointConfiguration()),
            Substitute.For<IConsumer>(),
            new KafkaOffset("topic", 2, 42));
    }

    [Benchmark(Baseline = true)]
    public void CreateTypedInboundEnvelopeViaActivator() => LegacyCreateTypedInboundEnvelope(
        _rawInboundEnvelope,
        _testEventOne,
        typeof(TestEventOne));

    [Benchmark]
    public void CreateTypedInboundEnvelope() => SerializationHelper.CreateTypedInboundEnvelope(
        _rawInboundEnvelope,
        _testEventOne,
        typeof(TestEventOne));

    [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Local", Justification = "test code")]
    private static IInboundEnvelope LegacyCreateTypedInboundEnvelope(
        IRawInboundEnvelope rawInboundEnvelope,
        object? deserializedMessage,
        Type messageType) =>
        (InboundEnvelope)Activator.CreateInstance(
            typeof(InboundEnvelope<>).MakeGenericType(messageType),
            rawInboundEnvelope,
            deserializedMessage)!;
}
