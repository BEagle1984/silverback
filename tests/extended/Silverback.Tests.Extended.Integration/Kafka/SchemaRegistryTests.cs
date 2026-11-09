// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Avro.Generic;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Extended.Integration.TestHost.Kafka;
using Silverback.Tests.Extended.Integration.TestHost.Kafka.SchemaRegistry;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Extended.Integration.Kafka;

[Collection(SchemaRegistryCollection.Name)]
[Trait("Type", "Integration")]
[Trait("Dependency", "Docker")]
[Trait("Broker", "Kafka")]
public class SchemaRegistryTests
{
    private const int MessagesCount = 5;

    private const byte MagicByte = 0;

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private static readonly string[] ExpectedNumbers = [.. Enumerable.Range(1, MessagesCount).Select(number => $"{number}")];

    private readonly ITestOutputHelper _output;

    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    public SchemaRegistryTests(SchemaRegistryFixture fixture, ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ProduceAndConsume_ShouldRoundTripAvro_WhenAutoRegisteringSchema()
    {
        string topic = await CreateTopicAsync();
        ConcurrentQueue<object> consumed = new();
        using CollectingLoggerProvider logs = new();
        using IHost host = CreateHost(
            topic,
            consumed,
            logs,
            producer => producer.Produce<AvroTestMessage>(endpoint => endpoint
                .SerializeAsAvro(avro => avro.ConnectToSchemaRegistry(SchemaRegistryFixture.Url))
                .ProduceTo(topic)),
            consumer => consumer.Consume<AvroTestMessage>(endpoint => endpoint
                .DeserializeAvro(avro => avro.ConnectToSchemaRegistry(SchemaRegistryFixture.Url))
                .ConsumeFrom(topic)));

        await RunAsync(
            host,
            logs,
            async () =>
            {
                await PublishAsync(host, number => new AvroTestMessage { Number = number });
                await WaitUntilConsumedAsync(consumed);
            });

        consumed.Cast<AvroTestMessage>().Select(message => message.Number).ShouldBe(ExpectedNumbers);
        logs.Errors.ShouldBeEmpty();

        using ISchemaRegistryClient registry = CreateRegistryClient();
        RegisteredSchema schema = await registry.GetLatestSchemaAsync(GetValueSubject(topic));
        schema.SchemaType.ShouldBe(SchemaType.Avro);
        schema.Version.ShouldBe(1);
        (await ReadSchemaIdsAsync(topic)).ShouldAllBe(id => id == schema.Id);
    }

    [Fact]
    public async Task ProduceAndConsume_ShouldRoundTripJson_WhenAutoRegisteringSchema()
    {
        string topic = await CreateTopicAsync();
        ConcurrentQueue<object> consumed = new();
        using CollectingLoggerProvider logs = new();
        using IHost host = CreateHost(
            topic,
            consumed,
            logs,
            producer => producer.Produce<JsonTestMessage>(endpoint => endpoint
                .SerializeAsJsonUsingSchemaRegistry(json => json.ConnectToSchemaRegistry(SchemaRegistryFixture.Url))
                .ProduceTo(topic)),
            consumer => consumer.Consume<JsonTestMessage>(endpoint => endpoint
                .DeserializeJsonUsingSchemaRegistry(json => json.ConnectToSchemaRegistry(SchemaRegistryFixture.Url))
                .ConsumeFrom(topic)));

        await RunAsync(
            host,
            logs,
            async () =>
            {
                await PublishAsync(host, number => new JsonTestMessage { Number = number });
                await WaitUntilConsumedAsync(consumed);
            });

        consumed.Cast<JsonTestMessage>().Select(message => message.Number).ShouldBe(ExpectedNumbers);
        logs.Errors.ShouldBeEmpty();

        using ISchemaRegistryClient registry = CreateRegistryClient();
        RegisteredSchema schema = await registry.GetLatestSchemaAsync(GetValueSubject(topic));
        schema.SchemaType.ShouldBe(SchemaType.Json);
        (await ReadSchemaIdsAsync(topic)).ShouldAllBe(id => id == schema.Id);
    }

    [Fact]
    public async Task ProduceAndConsume_ShouldRoundTripProtobuf_WhenAutoRegisteringSchema()
    {
        string topic = await CreateTopicAsync();
        ConcurrentQueue<object> consumed = new();
        using CollectingLoggerProvider logs = new();
        using IHost host = CreateHost(
            topic,
            consumed,
            logs,
            producer => producer.Produce<ProtobufTestMessage>(endpoint => endpoint
                .SerializeAsProtobuf(protobuf => protobuf.ConnectToSchemaRegistry(SchemaRegistryFixture.Url))
                .ProduceTo(topic)),
            consumer => consumer.Consume<ProtobufTestMessage>(endpoint => endpoint
                .DeserializeProtobuf(protobuf => protobuf.ConnectToSchemaRegistry(SchemaRegistryFixture.Url))
                .ConsumeFrom(topic)));

        await RunAsync(
            host,
            logs,
            async () =>
            {
                await PublishAsync(host, number => new ProtobufTestMessage { Number = number });
                await WaitUntilConsumedAsync(consumed);
            });

        consumed.Cast<ProtobufTestMessage>().Select(message => message.Number).ShouldBe(ExpectedNumbers);
        logs.Errors.ShouldBeEmpty();

        using ISchemaRegistryClient registry = CreateRegistryClient();
        RegisteredSchema schema = await registry.GetLatestSchemaAsync(GetValueSubject(topic));
        schema.SchemaType.ShouldBe(SchemaType.Protobuf);
        schema.SchemaString.ShouldContain("message ProtobufTestMessage");
        (await ReadSchemaIdsAsync(topic)).ShouldAllBe(id => id == schema.Id);
    }

    [Fact]
    public async Task Produce_ShouldUseRegisteredSchema_WhenAutoRegistrationIsDisabled()
    {
        string topic = await CreateTopicAsync();
        using ISchemaRegistryClient registry = CreateRegistryClient();
        int schemaId = await registry.RegisterSchemaAsync(
            GetValueSubject(topic),
            new Schema(AvroTestMessage._SCHEMA.ToString(), SchemaType.Avro));

        ConcurrentQueue<object> consumed = new();
        using CollectingLoggerProvider logs = new();
        using IHost host = CreateHost(
            topic,
            consumed,
            logs,
            producer => producer.Produce<AvroTestMessage>(endpoint => endpoint
                .SerializeAsAvro(avro => avro
                    .ConnectToSchemaRegistry(SchemaRegistryFixture.Url)
                    .Configure(config => config.DisableSchemaAutoRegistration()))
                .ProduceTo(topic)),
            consumer => consumer.Consume<AvroTestMessage>(endpoint => endpoint
                .DeserializeAvro(avro => avro.ConnectToSchemaRegistry(SchemaRegistryFixture.Url))
                .ConsumeFrom(topic)));

        await RunAsync(
            host,
            logs,
            async () =>
            {
                await PublishAsync(host, number => new AvroTestMessage { Number = number });
                await WaitUntilConsumedAsync(consumed);
            });

        consumed.Cast<AvroTestMessage>().Select(message => message.Number).ShouldBe(ExpectedNumbers);
        logs.Errors.ShouldBeEmpty();

        (await ReadSchemaIdsAsync(topic)).ShouldAllBe(id => id == schemaId);
        (await registry.GetSubjectVersionsAsync(GetValueSubject(topic))).ShouldBe([1]);
    }

    [Fact]
    public async Task Produce_ShouldThrow_WhenSchemaIsNotRegisteredAndAutoRegistrationIsDisabled()
    {
        string topic = await CreateTopicAsync();
        using CollectingLoggerProvider logs = new();
        using IHost host = CreateHost(
            topic,
            new ConcurrentQueue<object>(),
            logs,
            producer => producer.Produce<AvroTestMessage>(endpoint => endpoint
                .SerializeAsAvro(avro => avro
                    .ConnectToSchemaRegistry(SchemaRegistryFixture.Url)
                    .Configure(config => config.DisableSchemaAutoRegistration()))
                .ProduceTo(topic)),
            configureConsumer: null);

        SchemaRegistryException? exception = null;

        await RunAsync(
            host,
            logs,
            async () =>
            {
                IPublisher publisher = host.Services.GetRequiredService<IPublisher>();
                exception = await Should.ThrowAsync<SchemaRegistryException>(() => publisher.PublishAsync(new AvroTestMessage { Number = "1" }));
            });

        _output.WriteLine($"Exception: {exception}");
        exception.ShouldNotBeNull().Status.ShouldBe(HttpStatusCode.NotFound);

        using ISchemaRegistryClient registry = CreateRegistryClient();
        (await registry.GetAllSubjectsAsync()).ShouldNotContain(GetValueSubject(topic));
        (await ReadSchemaIdsAsync(topic, expectedCount: 0)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Consume_ShouldDeserializeAvro_WhenProducedWithConfluentSerializer()
    {
        string topic = await CreateTopicAsync();
        using ISchemaRegistryClient registry = CreateRegistryClient();
        using (IProducer<Null, AvroTestMessage> producer = new ProducerBuilder<Null, AvroTestMessage>(CreateProducerConfig())
                   .SetValueSerializer(new AvroSerializer<AvroTestMessage>(registry))
                   .Build())
        {
            foreach (string number in ExpectedNumbers)
            {
                await producer.ProduceAsync(topic, new Message<Null, AvroTestMessage> { Value = new AvroTestMessage { Number = number } });
            }
        }

        ConcurrentQueue<object> consumed = new();
        using CollectingLoggerProvider logs = new();
        using IHost host = CreateHost(
            topic,
            consumed,
            logs,
            configureProducer: null,
            consumer => consumer.Consume<AvroTestMessage>(endpoint => endpoint
                .DeserializeAvro(avro => avro.ConnectToSchemaRegistry(SchemaRegistryFixture.Url))
                .ConsumeFrom(topic)));

        await RunAsync(host, logs, () => WaitUntilConsumedAsync(consumed));

        consumed.Cast<AvroTestMessage>().Select(message => message.Number).ShouldBe(ExpectedNumbers);
        logs.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Produce_ShouldSerializeAvroReadableWithConfluentDeserializer()
    {
        string topic = await CreateTopicAsync();
        using CollectingLoggerProvider logs = new();
        using IHost host = CreateHost(
            topic,
            new ConcurrentQueue<object>(),
            logs,
            producer => producer.Produce<AvroTestMessage>(endpoint => endpoint
                .SerializeAsAvro(avro => avro.ConnectToSchemaRegistry(SchemaRegistryFixture.Url))
                .ProduceTo(topic)),
            configureConsumer: null);

        await RunAsync(host, logs, () => PublishAsync(host, number => new AvroTestMessage { Number = number }));

        logs.Errors.ShouldBeEmpty();

        // Read the generic records, to resolve the writer schema from the registry by the id in the payload, independently of the
        // message type
        using ISchemaRegistryClient registry = CreateRegistryClient();
        IReadOnlyList<GenericRecord> records = await ConsumeNativeAsync(
            topic,
            new AvroDeserializer<GenericRecord>(registry).AsSyncOverAsync(),
            MessagesCount);

        records.Select(record => record.Schema.Fullname).ShouldAllBe(name => name == typeof(AvroTestMessage).FullName);
        records.Select(record => (string)record["number"]).ShouldBe(ExpectedNumbers);
    }

    private static string GetValueSubject(string topic) => topic + "-value";

    private static ISchemaRegistryClient CreateRegistryClient() =>
        new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = SchemaRegistryFixture.Url });

    private static ProducerConfig CreateProducerConfig() =>
        new()
        {
            BootstrapServers = KafkaFixture.BootstrapServers,
            Acks = Acks.All
        };

    private static async Task<string> CreateTopicAsync()
    {
        string topic = "stress-schema-" + Guid.NewGuid().ToString("N");

        // A single partition, to verify the order of the consumed messages
        using (IAdminClient admin = CreateAdminClient(KafkaFixture.BootstrapServers))
        {
            await admin.CreateTopicsAsync([new TopicSpecification { Name = topic, NumPartitions = 1, ReplicationFactor = 1 }]);
        }

        // CreateTopics can return before every broker knows the topic, and a consumer joining the group in the meantime can receive an
        // empty assignment that is never repaired
        foreach (string broker in KafkaFixture.BootstrapServers.Split(','))
        {
            await WaitUntilBrokerKnowsTopicAsync(broker, topic);
        }

        return topic;
    }

    private static IAdminClient CreateAdminClient(string bootstrapServers) =>
        new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers }).Build();

    private static async Task WaitUntilBrokerKnowsTopicAsync(string broker, string topic)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (true)
        {
            try
            {
                // A new client is connected only to its bootstrap broker, so the metadata reflects that broker's view
                using IAdminClient admin = CreateAdminClient(broker);
                TopicMetadata? metadata = admin.GetMetadata(topic, TimeSpan.FromSeconds(5)).Topics.SingleOrDefault();

                if (metadata is { Error.IsError: false, Partitions: [{ Leader: >= 0 }] })
                    return;
            }
            catch (KafkaException)
            {
                // The metadata request can time out while the broker is applying the change
            }

            stopwatch.Elapsed.ShouldBeLessThan(Timeout, $"Timed out waiting for {broker} to know {topic}");
            await Task.Delay(100);
        }
    }

    private static IHost CreateHost(
        string topic,
        ConcurrentQueue<object> consumed,
        ILoggerProvider logs,
        Action<KafkaProducerConfigurationBuilder>? configureProducer,
        Action<KafkaConsumerConfigurationBuilder>? configureConsumer)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders().AddProvider(logs);
        builder.Services.AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka().AddConfluentSchemaRegistry())
            .AddDelegateSubscriber<IInboundEnvelope>(envelope => consumed.Enqueue(envelope.Message!))
            .AddKafkaClients(clients =>
            {
                clients.WithBootstrapServers(KafkaFixture.BootstrapServers);

                if (configureProducer != null)
                {
                    clients.AddProducer(producer =>
                    {
                        producer.WithClientId(topic + "-producer");
                        configureProducer(producer);
                    });
                }

                if (configureConsumer != null)
                {
                    clients.AddConsumer(consumer =>
                    {
                        consumer
                            .WithGroupId(topic + "-group")
                            .WithClientId(topic + "-consumer")
                            .AutoResetOffsetToEarliest();
                        configureConsumer(consumer);
                    });
                }
            });

        return builder.Build();
    }

    private static async Task PublishAsync(IHost host, Func<string, object> createMessage)
    {
        IPublisher publisher = host.Services.GetRequiredService<IPublisher>();

        foreach (string number in ExpectedNumbers)
        {
            await publisher.PublishAsync(createMessage(number));
        }
    }

    private static async Task WaitUntilConsumedAsync(ConcurrentQueue<object> consumed)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (consumed.Count < MessagesCount)
        {
            stopwatch.Elapsed.ShouldBeLessThan(Timeout, $"Timed out waiting for the messages, consumed {consumed.Count}");
            await Task.Delay(100);
        }
    }

    // Reads the schema id that the Confluent wire format prepends to each value: the magic byte, then a big-endian 32-bit id
    private static async Task<IReadOnlyList<int>> ReadSchemaIdsAsync(string topic, int expectedCount = MessagesCount)
    {
        IReadOnlyList<byte[]> values = await ConsumeNativeAsync(topic, Deserializers.ByteArray, expectedCount);

        return
        [
            .. values.Select(value =>
            {
                value.Length.ShouldBeGreaterThan(5);
                value[0].ShouldBe(MagicByte);

                return BinaryPrimitives.ReadInt32BigEndian(value.AsSpan(1, 4));
            })
        ];
    }

    // Reads the topic from the beginning with a native consumer, until the expected messages are read or, if none is expected, the
    // end of the partition is reached
    private static Task<IReadOnlyList<TValue>> ConsumeNativeAsync<TValue>(string topic, IDeserializer<TValue> deserializer, int expectedCount) =>
        Task.Run<IReadOnlyList<TValue>>(
            () =>
            {
                using IConsumer<Ignore, TValue> consumer = new ConsumerBuilder<Ignore, TValue>(
                        new ConsumerConfig
                        {
                            BootstrapServers = KafkaFixture.BootstrapServers,
                            GroupId = topic + "-native",
                            EnableAutoCommit = false,
                            EnablePartitionEof = true
                        })
                    .SetValueDeserializer(deserializer)
                    .Build();
                consumer.Assign(new TopicPartitionOffset(topic, 0, Offset.Beginning));

                List<TValue> values = [];
                Stopwatch stopwatch = Stopwatch.StartNew();

                while (values.Count < expectedCount || expectedCount == 0)
                {
                    stopwatch.Elapsed.ShouldBeLessThan(Timeout, $"Timed out reading {topic}, read {values.Count}");
                    ConsumeResult<Ignore, TValue>? result = consumer.Consume(TimeSpan.FromMilliseconds(500));

                    if (result == null)
                        continue;

                    if (result.IsPartitionEOF)
                    {
                        if (expectedCount == 0)
                            break;

                        continue;
                    }

                    values.Add(result.Message.Value);
                }

                consumer.Close();

                return values;
            });

    private async Task RunAsync(IHost host, CollectingLoggerProvider logs, Func<Task> action)
    {
        try
        {
            await host.StartAsync().WaitAsync(Timeout);
            await action();
        }
        finally
        {
            try
            {
                await host.StopAsync().WaitAsync(Timeout);
            }
            finally
            {
                foreach (string entry in logs.Entries)
                {
                    _output.WriteLine($"Log: {entry}");
                }
            }
        }
    }

    private sealed class CollectingLoggerProvider : ILoggerProvider, ILogger
    {
        public ConcurrentQueue<string> Entries { get; } = new();

        public ConcurrentQueue<string> Errors { get; } = new();

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            string entry = $"{logLevel} {eventId.Id}: {formatter(state, exception)}";

            if (exception != null)
                entry += $" | {exception.GetType().Name}: {exception.Message}";

            Entries.Enqueue(entry);

            if (logLevel >= LogLevel.Error)
                Errors.Enqueue(entry);
        }

        public void Dispose()
        {
            // No resources to release
        }
    }
}
