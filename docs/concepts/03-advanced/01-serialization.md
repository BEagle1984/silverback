---
uid: serialization
---

# Serialization

Being flexible when serializing and deserializing the messages sent over the message broker is crucial for interoperability and these mechanisms are therfore completely customizable.

## Default JSON serialization

The default [`JsonMessageSerializer`](xref:Silverback.Messaging.Serialization.JsonMessageSerializer) internally uses System.Text.Json to serialize the messages as JSON and encode them in UTF-8.

A few headers are added to the message, in particular `x-message-type` is used by the [`JsonMessageSerializer`](xref:Silverback.Messaging.Serialization.JsonMessageSerializer) to know the message type when deserializing. It also leverages the Newtonsoft's [automatic type handling](https://www.newtonsoft.com/json/help/html/SerializeTypeNameHandling.htm) to automatically resolve the actual type of the nested properties.

The deserializer function provided by [`JsonMessageSerializer`](xref:Silverback.Messaging.Serialization.JsonMessageSerializer) will obviously try to map the message to a type with the exact assembly qualified name found in the `x-message-type` header. It is therefore a good practice to share the message models among the services, maybe through a nuget package.

This is the suggested serialization strategy when both producer and consumer are based on Silverback but may not be ideal for interoperability.

Have a look at the <xref:headers> section for an overview on the headers that are appended to the messages.

## Fixed-type JSON for interoperability

If you are consuming a message coming from another system (not based on Silverback), chances are that the type name is not being delivered as header.

In that case you can resort to the typed [`JsonMessageSerializer<TMessage>`](xref:Silverback.Messaging.Serialization.JsonMessageSerializer`1). This serializer works like the default one but the message type is hard-coded, instead of being expected in the header.

```csharp
public class Startup
{
    public void Configure(IBusConfigurator busConfigurator)
    {
        busConfigurator
            .Connect(endpoints => endpoints
                .AddInbound(
                    new KafkaConsumerEndpoint("order-events")
                    {
                        Serializer = new JsonMessageSerializer<OrderEvent>
                    }));
    }
}
```

> [!Note]
> The [`JsonMessageSerializer`](xref:Silverback.Messaging.Serialization.JsonMessageSerializer) can be also be tweaked modifying its `Options`.

## Newtonsoft.Json

In the releases previous to 3.0.0 the default [`JsonMessageSerializer`](xref:Silverback.Messaging.Serialization.JsonMessageSerializer) was based on Newtonsoft.Json instead of System.Text.Json. For backward compatibility reasons and since System.Text.Json may not support all use cases covered by Newtonsoft.Json, the old serializers have been renamed to [`NewtonsoftJsonMessageSerializer`](xref:Silverback.Messaging.Serialization.NewtonsoftJsonMessageSerializer`1)/[`NewtonsoftJsonMessageSerializer<TMessage>`](xref:Silverback.Messaging.Serialization.NewtonsoftJsonMessageSerializer`1) and moved into a dedicated NuGet package: Silverback.Integration.Newtonsoft.

## Apache Avro

The [`AvroMessageSerializer<TMessage>`](xref:Silverback.Messaging.Serialization.AvroMessageSerializer`1) contained in the Silverback.Integration.Kafka.SchemaRegistry package can be used to connect with a schema registry and exchange messages in [Apache Avro](https://avro.apache.org/) format.

```csharp
public class Startup
{
    public void Configure(IBusConfigurator busConfigurator)
    {
        busConfigurator
            .Connect(endpoints => endpoints
                .AddOutbound<OrderEvent>(
                    new KafkaConsumerEndpoint("order-events")
                    {
                        Serializer = new AvroMessageSerializer<OrderEvent>
                        {
                            SchemaRegistryConfig = new SchemaRegistryConfig
                            {
                                Url = "schema-registry:8081"
                            },
                            AvroSerializerConfig = new AvroSerializerConfig
                            {
                                AutoRegisterSchemas = true
                            }
                        },
                    }));
    }
}
```

> [!Note]
> The C# message models can be generated from an Avro schema using [AvroGen](https://www.nuget.org/packages/Confluent.Apache.Avro.AvroGen/).

> [!Note]
> This serializer is built for Kafka but it could work with other brokers, as long as a schema registry is available.

## Custom serializer

In some cases you may want to build your very own custom serializer implementing `IMessageSerializer` directly.

```csharp
public class MyCustomSerializer : IMessageSerializer
{
    public byte[] Serialize(object message, MessageHeaderCollection messageHeaders)
    {
        ...
    }

    public object Deserialize(byte[] message, MessageHeaderCollection messageHeaders)
    {
        ...
    }

    public byte[] Serialize(
        object message,
        MessageHeaderCollection messageHeaders,
        MessageSerializationContext context)
    {
        ...
    }

    public object Deserialize(
        byte[] message,
        MessageHeaderCollection messageHeaders,
        MessageSerializationContext context)
    {
        ...
    }

    public Task<byte[]> SerializeAsync(
        object message,
        MessageHeaderCollection messageHeaders,
        MessageSerializationContext context)
    {
        ...
    }

    public Task<object> DeserializeAsync(
        byte[] message,
        MessageHeaderCollection messageHeaders,
        MessageSerializationContext context)
    {
        ...
    }
}
```

> [!Note]
> You may need to implement `IKafkaMessageSerializer` if you want to have full control over the serialization of the Kafka key as well.

## Binary Files

Please refer to the <xref:binary-files> page if you need to prduce or consume raw binary files.