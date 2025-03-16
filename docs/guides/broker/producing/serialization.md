---
uid: serialization
---

# Serializing the Produced Messages

The message payload needs to be serialized into a byte array before it can be sent to the broker. By default, Silverback uses `System.Text.Json` to serialize the message payload. This can be customized tweaking the serializer settings, using another one of the built-in serializers or implementing a custom serializer.

## JSON

### System.Text.Json

The default serializer is `System.Text.Json`. It is used to serialize the message payload into a JSON string. The serializer settings can be customized using the `Configure` method.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic")
                .SerializeAsJson(serializer => serializer
                    .Configure(
                        options =>
                        {
                            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                        })))));
```

### Newtonsoft.Json

Silverback also provides a serializer based on `Newtonsoft.Json`. It is found in the [Silverback.Newtonsoft](https://www.nuget.org/packages/Silverback.Newtonsoft/) package and can be enabled by calling the `SerializeAsJsonUsingNewtonsoft` method.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic")
                .SerializeAsJsonUsingNewtonsoft())));
```

Optionally, the serializer settings can be customized using the `Configure` method.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic")
                .SerializeAsJsonUsingNewtonsoft(serializer => serializer
                    .Configure(
                        settings =>
                        {
                            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                        }))));
```

### Schema Registry

To integrate with Confluent Schema Registry, you can use the dedicated JSON serializer designed for schema registry support found in the [Silverback.Kafka.SchemaRegistry](https://www.nuget.org/packages/Silverback.Kafka.SchemaRegistry/) package. This uses the Confluent serializer under the hood, which in turn uses `Newtonsoft.Json`.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic")
                .SerializeAsJsonUsingSchemaRegistry(serializer => serializer
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

The raw serializer is not a traditional serializer. Instead, it sends the message payload exactly as it is, without any transformation. This is particularly useful when the payload is already a byte array or a stream and does not require additional serialization.

To publish a raw message, wrap the byte array or stream in one of the following types:
* <xref:Silverback.Messaging.Messages.RawMessage>
* <xref:Silverback.Messaging.Messages.RawMessage`1>
* A custom class derived from <xref:Silverback.Messaging.Messages.RawMessage>

The generic type parameter in <xref:Silverback.Messaging.Messages.RawMessage`1> is used solely for message routing purposes, ensuring that the message is delivered to the correct endpoint, just as a custom derived class would.

When the producer endpoint is configured to route a <xref:Silverback.Messaging.Messages.RawMessage> or a derived class, the raw serializer is used by default.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<RawMessage<MyMessage>>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic"))));
```

### Binary

The binary serializer is similar to the raw serializer but supports the `x-message-type` header, meaning that these messages can be mixed with other (JSON-serialized) messages on the same topic and Silverback will be able to discriminate them.

To publish a binary message, wrap the byte array or stream in one of the following types:
* <xref:Silverback.Messaging.Messages.BinaryMessage>
* A custom class derived from <xref:Silverback.Messaging.Messages.BinaryMessage>
* A custom class implementing <xref:Silverback.Messaging.Messages.IBinaryMessage>

When the producer endpoint is configured to route a type implementing <xref:Silverback.Messaging.Messages.IBinaryMessage>, the binary serializer is used by default.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyBinaryMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic"))));
```

## String

The string serializer encodes a raw string into a byte array.

To publish a string message, wrap the string in one of the following types:
* <xref:Silverback.Messaging.Messages.StringMessage>
* <xref:Silverback.Messaging.Messages.StringMessage`1>
* A custom class derived from <xref:Silverback.Messaging.Messages.StringMessage>

The generic type parameter in <xref:Silverback.Messaging.Messages.StringMessage`1> is used solely for message routing purposes, ensuring that the message is delivered to the correct endpoint, just as a custom derived class would.

When the producer endpoint is configured to route a <xref:Silverback.Messaging.Messages.StringMessage> or a derived class, the string serializer is used by default.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<StringMessage<MyMessage>>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic"))));
```

The default encoding is UTF-8, but it can be customized using the `WithEncoding` method.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<StringMessage<MyMessage>>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic")
                .ProduceStrings(serializer => serializer
                    .WithEncoding(MessageEncoding.Unicode)))));
```

## Avro

To serialize in Avro format you can use the Avro serializer found in the [Silverback.Kafka.SchemaRegistry](https://www.nuget.org/packages/Silverback.Kafka.SchemaRegistry/) package. This serializer is based on the Confluent Avro serializer and requires a schema registry.

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

> [!Note]
> The C# message models can be generated from an Avro schema using [AvroGen](https://www.nuget.org/packages/Confluent.Apache.Avro.AvroGen/).

> [!Note]
> To learn more about the schema registry support, refer to the <xref:schema-registry> guide.

## Protobuf

To serialize in Protobuf format you can use the Protobuf serializer found in the [Silverback.Kafka.SchemaRegistry](https://www.nuget.org/packages/Silverback.Kafka.SchemaRegistry/) package. This serializer is based on the Confluent Protobuf serializer and requires a schema registry.

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

> [!Note]
> To learn more about the schema registry support, refer to the <xref:schema-registry> guide.

## Custom Serializer

To implement a custom serializer, create a class implementing the <xref:Silverback.Messaging.Serialization.IMessageSerializer> interface.

```csharp
public class MyCustomSerializer : IMessageSerializer
{
    public ValueTask<Stream?> SerializeAsync(object? message, MessageHeaderCollection headers, ProducerEndpoint endpoint)
    {
        // Implement the serialization logic here
    }
}
```

The custom serializer can be plugged in calling the `SerializeUsing` method.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic")
                .SerializeUsing(new MyCustomSerializer()))));
```

## Additional Resources

* [API Reference](xref:Silverback)
* <xref:samples>
* <xref:deserialization> guide

