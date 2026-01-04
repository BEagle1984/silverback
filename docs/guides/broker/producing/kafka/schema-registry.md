---
uid: kafka-schema-registry-serializer
---

# Kafka Schema Registry

Use Confluent Schema Registry to validate and evolve message schemas for Kafka topics. Silverback integrates with Schema Registry via `Silverback.Integration.Kafka.SchemaRegistry` and provides endpoint-level serialization for Avro, JSON Schema, and Protobuf.

## Prerequisites

- A running Confluent Schema Registry instance.
- The `Silverback.Integration.Kafka.SchemaRegistry` package referenced by your application.

## Configure Producer Endpoints

Configure Schema Registry per endpoint using one of:

- `SerializeAsAvro`
- `SerializeAsJsonUsingSchemaRegistry`
- `SerializeAsProtobuf`

Example (JSON Schema):

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic")
                .SerializeAsJsonUsingSchemaRegistry(json => json
                    .ConnectToSchemaRegistry("http://localhost:4242")
                    .Configure(config =>
                    {
                        config.AutoRegisterSchemas = false;
                    })))));
```

> [!Note]
> Consumer setup is documented in <xref:kafka-schema-registry-deserializer>.

## Supported Serializers

### JSON Schema

Use `SerializeAsJsonUsingSchemaRegistry`:

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic")
                .SerializeAsJsonUsingSchemaRegistry(serializer => serializer
                    .ConnectToSchemaRegistry("http://localhost:4242")))));
```

### Avro

Use `SerializeAsAvro`:

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic")
                .SerializeAsAvro(serializer => serializer
                    .ConnectToSchemaRegistry("http://localhost:4242")))));
```

> [!Tip]
> You can generate C# models from an Avro schema using [AvroGen](https://www.nuget.org/packages/Confluent.Apache.Avro.AvroGen/).

### Protobuf

Use `SerializeAsProtobuf`:

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic")
                .SerializeAsProtobuf(serializer => serializer
                    .ConnectToSchemaRegistry("http://localhost:4242")))));
```

## Schema Registration

In most setups, schemas are registered by producers.

If you disable automatic schema registration (`AutoRegisterSchemas = false`), ensure schemas are registered before producing messages.

You can register schemas programmatically using <xref:Silverback.Messaging.Serialization.IConfluentSchemaRegistryClientFactory>:

```csharp
class SchemaRegistrationService
{
    private readonly IConfluentSchemaRegistryClientFactory _factory;

    public SchemaRegistrationService(IConfluentSchemaRegistryClientFactory factory)
    {
        _factory = factory;
    }

    public async Task RegisterSchemasAsync(string formattedSchema, string topicName)
    {
        ISchemaRegistryClient client = _factory.GetClient(registry => registry
            .WithUrl("http://localhost:4242"));

        await client.RegisterSchemaAsync(
            topicName + "-value",
            new Schema(formattedSchema, SchemaType.Protobuf));
    }
}
```

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:serialization>
- <xref:kafka-schema-registry-deserializer>
- [Confluent Schema Registry](https://docs.confluent.io/platform/current/schema-registry/index.html)
