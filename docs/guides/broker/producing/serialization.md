---
uid: serialization
description: "Serialize produced messages with System.Text.Json, Newtonsoft.Json, Avro, Protobuf, raw binary or strings, or plug in a custom serializer."
---

# Serializing the Produced Messages

The message payload needs to be serialized into a byte array before it can be sent to the broker. By default, Silverback uses `System.Text.Json` to serialize the message payload. This can be customized tweaking the serializer settings, using another one of the built-in serializers or implementing a custom serializer.

## JSON

### System.Text.Json

The default serializer is `System.Text.Json`. It is used to serialize the message payload into a JSON string. The serializer settings can be customized using the `Configure` method. When called multiple times, each call applies its changes on top of the previous ones.

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

#### Source Generation

The serializer can use the metadata generated at compile time by the [System.Text.Json source generator](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/source-generation) instead of runtime reflection. Declare a `JsonSerializerContext` listing the message types and pass it to the `UseJsonSerializerContext` method.

```csharp
[JsonSerializable(typeof(MyMessage))]
[JsonSerializable(typeof(MyOtherMessage))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class MyJsonContext : JsonSerializerContext;
```

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<IMyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic")
                .SerializeAsJson(serializer => serializer
                    .UseJsonSerializerContext(MyJsonContext.Default)))));
```

The serializer uses the context's own options, which for the generated `Default` instance include the settings declared via `[JsonSourceGenerationOptions]`. Calling `Configure` afterwards customizes a copy of these options, for example to combine several contexts in the `TypeInfoResolverChain`. The resolvers are queried in order and the first one providing the metadata for the type wins. All contexts use the same options, so most settings declared via `[JsonSourceGenerationOptions]` on the additional contexts, such as the naming policy, are ignored (see below).

```csharp
.SerializeAsJson(serializer => serializer
    .UseJsonSerializerContext(MyJsonContext.Default)
    .Configure(options => options.TypeInfoResolverChain.Add(MyOtherJsonContext.Default)))
```

Alternatively, the context can be set as `TypeInfoResolver` (or added to the `TypeInfoResolverChain`) in the `Configure` method. In this case, most settings declared via `[JsonSourceGenerationOptions]`, such as the naming policy, the ignore conditions or the case insensitivity, are ignored, because they apply only to the context's own `Options`, and have to be set in `Configure` as well. The same applies to the `Converters`, which have to be added to `options.Converters`. Only `UseStringEnumConverter` is part of the generated metadata and still applies.

```csharp
.SerializeAsJson(serializer => serializer
    .Configure(
        options =>
        {
            options.TypeInfoResolver = MyJsonContext.Default;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        }))
```

Keep the following in mind:
* Silverback serializes the actual type of the message, not the type declared in the endpoint configuration. Every concrete type that is produced must be listed in a context, otherwise the serializer throws a `NotSupportedException`. To fall back to reflection for the types not listed, add a `DefaultJsonTypeInfoResolver` at the end of the `TypeInfoResolverChain`.
* The `x-message-type` header is written as usual (see [Polymorphic Serialization](#polymorphic-serialization)).
* This reduces the reflection used for the serialization, but doesn't make Silverback compatible with trimming or Native AOT.

### Newtonsoft.Json

Silverback also provides a serializer based on `Newtonsoft.Json`. It is found in the [Silverback.Integration.Newtonsoft](https://www.nuget.org/packages/Silverback.Integration.Newtonsoft/) package and can be enabled by calling the `SerializeAsJsonUsingNewtonsoft` method.

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

Optionally, the serializer settings can be customized using the `Configure` method. When called multiple times, each call applies its changes on top of the previous ones.

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
                        })))));
```

### Polymorphic Serialization

Silverback by default adds an `x-message-type` header to the produced messages to allow the consumer to discriminate the message type and deserialize it accordingly. This allows polymorphic serialization, meaning that the producer can send messages of different types to the same topic and the consumer will be able to deserialize them correctly.

For example, if the producer is configured to produce messages of type `MyMessage` but it actually produces a message of type `MyDerivedMessage`, the `x-message-type` header will be set to the **assembly qualified name** of `MyDerivedMessage`.

This header will always be sent, even if the actual message type is the same as the one specified in the producer configuration. This header can be dropped calling `DisableTypeHeader` on the serializer builder (e.g. `.SerializeAsJson(serializer => serializer.DisableTypeHeader())`).

To handle polymorphic nested properties, you have instead to rely on the serializer built-in functionality (e.g., `TypeNameHandling` in `Newtonsoft.Json`).

### Schema Registry

To integrate with Confluent Schema Registry, you can use the dedicated JSON serializer designed for schema registry support found in the [Silverback.Integration.Kafka.SchemaRegistry](https://www.nuget.org/packages/Silverback.Integration.Kafka.SchemaRegistry/) package. This uses the Confluent serializer under the hood, which in turn uses `Newtonsoft.Json`.

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
                    .Configure(config => config.DisableSchemaAutoRegistration())))));
```

> [!Note]
> To learn more about the schema registry support, refer to the <xref:kafka-schema-registry-serializer> guide.

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

To serialize in Avro format you can use the Avro serializer found in the [Silverback.Integration.Kafka.SchemaRegistry](https://www.nuget.org/packages/Silverback.Integration.Kafka.SchemaRegistry/) package. This serializer is based on the Confluent Avro serializer and requires a schema registry.

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
> To learn more about the schema registry support, refer to the <xref:kafka-schema-registry-serializer> guide.

## Protobuf

To serialize in Protobuf format you can use the Protobuf serializer found in the [Silverback.Integration.Kafka.SchemaRegistry](https://www.nuget.org/packages/Silverback.Integration.Kafka.SchemaRegistry/) package. This serializer is based on the Confluent Protobuf serializer and requires a schema registry.

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
> To learn more about the schema registry support, refer to the <xref:kafka-schema-registry-serializer> guide.

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

- [API Reference](xref:Silverback)
- <xref:samples>
- <xref:deserialization>
