---
uid: kafka-schema-registry-serializer
---

# Confluent Schema Registry

The Confluent Schema Registry is a service that provides a centralized repository for managing schemas used in Kafka messages. It allows producers and consumers to validate and evolve schemas without breaking compatibility.

Silverback supports the Confluent Schema Registry, enabling you to use Avro, JSON, or Protobuf schemas for your Kafka messages. This integration ensures that your messages conform to the defined schemas, providing a robust way to manage message formats.

## Configuring the Schema Registry

### Producer Configuration

To configure a producer to use the Confluent Schema Registry, you need to use `SerializeAsAvro`, `SerializeAsJsonUsingSchemaRegistry`, or `SerializeAsProtobuf` methods in the endpoint configuration and specify at least the URL of the Schema Registry.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic")
                .SerializeAsJsonUsingSchemaRegistry(
                    json => json
                        .ConnectToSchemaRegistry("http://localhost:4242")
                        .Configure(
                            config =>
                            {
                                config.AutoRegisterSchemas = false;
                            })))));
```

### Consumer Configuration

To configure a consumer to use the Confluent Schema Registry, you can use the `DeserializeAvro`, `DeserializeJsonUsingSchemaRegistry`, or `DeserializeProtobuf` methods in the endpoint configuration.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic")
                .DeserializeAvro(avro => avro
                    .ConnectToSchemaRegistry("http://localhost:4242")))));
```

## Schema Registration

When using the Schema Registry, you can register schemas manually or let Silverback handle schema registration automatically. If you choose to register schemas manually (setting `AutoRegisterSchemas` to `false`), you must ensure that the schemas are registered in the Schema Registry before producing messages.

You can use the `IConfluentSchemaRegistryClientFactory` to get an instance of the `IConfluentSchemaRegistryClient` and register schemas programmatically.

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

* [API Reference](xref:Silverback)
* <xref:serialization> guide
* <xref:kafka-schema-registry-deserializer> guide
* [Confluent Schema Registry](https://docs.confluent.io/platform/current/schema-registry/index.html)
