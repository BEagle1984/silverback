---
uid: deserialization
---

# Deserializing the Consumed Messages

The message payload received from the broker needs to be deserialized from a byte array into the target message type before it can be processed by the subscribers. By default, Silverback uses `System.Text.Json` to deserialize the message payload. This can be customized by tweaking the deserializer settings, using another one of the built-in deserializers or implementing a custom deserializer.

## JSON

### System.Text.Json

The default deserializer is `System.Text.Json`. It is used to deserialize the message payload from a JSON string. The deserializer settings can be customized using the `Configure` method.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic")
                .DeserializeJson(deserializer => deserializer
                    .Configure(
                        options =>
                        {
                            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                        })))));
```

### Newtonsoft.Json

Silverback also provides a deserializer based on `Newtonsoft.Json`. It is found in the [Silverback.Newtonsoft](https://www.nuget.org/packages/Silverback.Newtonsoft/) package and can be enabled by calling the `DeserializeJsonUsingNewtonsoft` method.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic")
                .DeserializeJsonUsingNewtonsoft())));
```

Optionally, the deserializer settings can be customized using the `Configure` method.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic")
                .DeserializeJsonUsingNewtonsoft(deserializer => deserializer
                    .Configure(
                        settings =>
                        {
                            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                        }))));
```

### Schema Registry

To integrate with Confluent Schema Registry, you can use the dedicated JSON deserializer designed for schema registry support found in the [Silverback.Kafka.SchemaRegistry](https://www.nuget.org/packages/Silverback.Kafka.SchemaRegistry/) package. This uses the Confluent deserializer under the hood, which in turn uses `Newtonsoft.Json`.

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
                    .Configure(
                        config =>
                        {
                            config.AutoRegisterSchemas = false;
                        }))));
```

> [!Note]
> To learn more about the schema registry support, refer to the <xref:schema-registry> guide.

## Raw

The raw deserializer is not a traditional deserializer. Instead, it leaves the message payload as-is, returning the original byte array or stream. This is particularly useful when the payload is already a byte array or a stream and does not require transformation.

When consuming raw messages the consumer will provide one of the following types:
* <xref:Silverback.Messaging.Messages.RawMessage>
* <xref:Silverback.Messaging.Messages.RawMessage`1>
* A custom class derived from <xref:Silverback.Messaging.Messages.RawMessage>

The generic type parameter in <xref:Silverback.Messaging.Messages.RawMessage`1> is used solely for message routing purposes, ensuring that the message is delivered to the correct endpoint, just as a custom derived class would.

When the consumer endpoint is configured to route a <xref:Silverback.Messaging.Messages.RawMessage> or a derived class, the raw deserializer is used by default.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<RawMessage<MyMessage>>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic"))));
```

### Binary

The binary deserializer is similar to the raw deserializer but supports the `x-message-type` header, meaning that these messages can be mixed with other (JSON-deserialized) messages on the same topic and Silverback will be able to discriminate them.

When consuming binary messages use:
* <xref:Silverback.Messaging.Messages.BinaryMessage>
* A custom class derived from <xref:Silverback.Messaging.Messages.BinaryMessage>
* A custom class implementing <xref:Silverback.Messaging.Messages.IBinaryMessage>

When the consumer endpoint is configured to route a type implementing <xref:Silverback.Messaging.Messages.IBinaryMessage>, the binary deserializer is used by default.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyBinaryMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic"))));
```

## String

The string deserializer decodes a byte array into a raw string.

To consume a string message, use one of the following types:
* <xref:Silverback.Messaging.Messages.StringMessage>
* <xref:Silverback.Messaging.Messages.StringMessage`1>
* A custom class derived from <xref:Silverback.Messaging.Messages.StringMessage>

The generic type parameter in <xref:Silverback.Messaging.Messages.StringMessage`1> is used solely for message routing purposes, ensuring that the message is delivered to the correct endpoint, just as a custom derived class would.

When the consumer endpoint is configured to route a <xref:Silverback.Messaging.Messages.StringMessage> or a derived class, the string deserializer is used by default.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<StringMessage<MyMessage>>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic"))));
```

The default encoding is UTF-8, but it can be customized using the `WithEncoding` method.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<StringMessage<MyMessage>>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic")
                .DeserializeStrings(deserializer => deserializer
                    .WithEncoding(MessageEncoding.Unicode)))));
```

## Avro

To deserialize Avro formatted messages you can use the Avro deserializer found in the [Silverback.Kafka.SchemaRegistry](https://www.nuget.org/packages/Silverback.Kafka.SchemaRegistry/) package. This deserializer is based on the Confluent Avro deserializer and requires a schema registry.

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

> [!Note]
> The C# message models can be generated from an Avro schema using [AvroGen](https://www.nuget.org/packages/Confluent.Apache.Avro.AvroGen/).

> [!Note]
> To learn more about the schema registry support, refer to the <xref:schema-registry> guide.

## Protobuf

To deserialize Protobuf messages you can use the Protobuf deserializer found in the [Silverback.Kafka.SchemaRegistry](https://www.nuget.org/packages/Silverback.Kafka.SchemaRegistry/) package. This deserializer is based on the Confluent Protobuf deserializer and requires a schema registry.

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

> [!Note]
> To learn more about the schema registry support, refer to the <xref:schema-registry> guide.

## Custom Deserializer

To implement a custom deserializer, create a class implementing the <xref:Silverback.Messaging.Serialization.IMessageDeserializer> interface.

```csharp
public class MyCustomDeserializer : IMessageDeserializer
{
    public ValueTask<DeserializedMessage> DeserializeAsync(Stream? messageStream, MessageHeaderCollection headers, ConsumerEndpoint endpoint)
    {
        // Implement the deserialization logic here
    }
}
```

The custom deserializer can be plugged in calling the `DeserializeUsing` method.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic")
                .DeserializeUsing(new MyCustomDeserializer()))));
```

## Additional Resources

* [API Reference](xref:Silverback)
* <xref:samples>
* <xref:serialization> guide
