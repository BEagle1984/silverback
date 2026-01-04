---
uid: kafka-schema-registry-deserializer
---

# Kafka Schema Registry

Use Confluent Schema Registry to validate and evolve message schemas for Kafka topics. Silverback integrates via `Silverback.Integration.Kafka.SchemaRegistry` and supports Avro, JSON Schema, and Protobuf.

## Prerequisites

- A running Confluent Schema Registry instance.
- The `Silverback.Integration.Kafka.SchemaRegistry` package referenced by your application.

## Configure Consumer Endpoints

Configure Schema Registry per consumer endpoint using one of:

- `DeserializeAvro`
- `DeserializeJsonUsingSchemaRegistry`
- `DeserializeProtobuf`

Example (JSON Schema):

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic")
                .DeserializeJsonUsingSchemaRegistry(deserializer => deserializer
                    .ConnectToSchemaRegistry("http://localhost:4242")
                    .Configure(config =>
                    {
                        config.AutoRegisterSchemas = false;
                    })))));
```

> [!Note]
> Producer setup is documented in <xref:kafka-schema-registry-serializer>.

## Supported Deserializers

### JSON Schema

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic")
                .DeserializeJsonUsingSchemaRegistry(deserializer => deserializer
                    .ConnectToSchemaRegistry("http://localhost:4242")))));
```

### Avro

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic")
                .DeserializeAvro(deserializer => deserializer
                    .ConnectToSchemaRegistry("http://localhost:4242")))));
```

> [!Tip]
> You can generate C# models from an Avro schema using [AvroGen](https://www.nuget.org/packages/Confluent.Apache.Avro.AvroGen/).

### Protobuf

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic")
                .DeserializeProtobuf(deserializer => deserializer
                    .ConnectToSchemaRegistry("http://localhost:4242")))));
```

## Schema Registration

In most setups, schemas are registered by producers and consumers only read existing schemas.

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
- <xref:deserialization>
- <xref:kafka-schema-registry-serializer>
- [Confluent Schema Registry](https://docs.confluent.io/platform/current/schema-registry/index.html)
