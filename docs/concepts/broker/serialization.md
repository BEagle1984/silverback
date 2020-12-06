---
uid: serialization
---

# Serialization

Being flexible when serializing and deserializing the messages sent over the message broker is crucial for interoperability and these mechanisms are therefore completely customizable.

## Default JSON serialization

The default <xref:Silverback.Messaging.Serialization.JsonMessageSerializer> internally uses [System.Text.Json](https://www.nuget.org/packages/System.Text.Json) to serialize the messages as JSON and encode them in UTF-8.

A few headers are added to the message, in particular `x-message-type` is used by the <xref:Silverback.Messaging.Serialization.JsonMessageSerializer> to know the message type when deserializing it in the consumer, thus allowing messages of different types being sent over the same topic or queue.

> [!Warning]
> The <xref:Silverback.Messaging.Serialization.JsonMessageSerializer> will obviously try to map the message to a type with the exact assembly qualified name found in the `x-message-type` header. It is therefore a good practice to share the message models among the services, maybe through a shared project or a nuget package.

This is the suggested serialization strategy when both producer and consumer are based on Silverback but may not be ideal for interoperability.

Have a look at the <xref:headers> section for an overview on the headers that are appended to the messages.

## Fixed-type JSON for interoperability

If you are consuming a message coming from another system (not based on Silverback), chances are that the message type name is not being delivered as header.

In that case you can resort to the typed <xref:Silverback.Messaging.Serialization.JsonMessageSerializer`1>. This serializer works like the default one seen in the previous chapter, but the message type is hard-coded, instead of being resolved according to the message header.

# [Fluent](#tab/json-fixed-type-fluent)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddOutbound<InventoryEvent>(endpoint => endpoint
                    .ProduceTo("inventory-events")
                    .SerializeAsJson(serializer => serializer
                        .UseFixedType<InventoryEvent>()))
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("order-events")
                    .Configure(config => 
                        {
                            config.GroupId = "my-consumer";
                        })
                    .DeserializeJson(serializer => serializer
                        .UseFixedType<OrderEvent>())));
}
```
# [Legacy](#tab/json-fixed-type-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddOutbound<InventoryEvent>(
                new KafkaProducerEndpoint("inventory-events")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092"
                    },
                    Serializer = new JsonMessageSerializer<InventoryEvent>() 
                })
            .AddInbound(
                new KafkaConsumerEndpoint("order-events")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092",
                        GroupId = "my-consumer"
                    },
                    Serializer = new JsonMessageSerializer<OrderEvent>()
                });
}
```
***

## JSON using Newtonsoft.Json

Prior to release 3.0.0 the default <xref:Silverback.Messaging.Serialization.JsonMessageSerializer> was based on [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json) instead of [System.Text.Json](https://www.nuget.org/packages/System.Text.Json). For backward compatibility reasons and since [System.Text.Json](https://www.nuget.org/packages/System.Text.Json) may not support all use cases covered by [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json), the old serializers have been renamed to <xref:Silverback.Messaging.Serialization.NewtonsoftJsonMessageSerializer> and <xref:Silverback.Messaging.Serialization.NewtonsoftJsonMessageSerializer`1> and moved into the dedicated [Silverback.Integration.Newtonsoft](https://www.nuget.org/packages/Silverback.Integration.Newtonsoft) package.

# [Fluent](#tab/newtonsoft-fluent)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddOutbound<InventoryEvent>(endpoint => endpoint
                    .ProduceTo("inventory-events")
                    .SerializeAsJsonUsingNewtonsoft())
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("order-events")
                    .Configure(config => 
                        {
                            config.GroupId = "my-consumer";
                        })
                    .DeserializeJsonUsingNewtonsoft()));
}
```
# [Legacy](#tab/newtonsoft-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddOutbound<InventoryEvent>(
                new KafkaProducerEndpoint("inventory-events")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092"
                    },
                    Serializer = new NewtonsoftJsonMessageSerializer() 
                })
            .AddInbound(
                new KafkaConsumerEndpoint("order-events")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092",
                        GroupId = "my-consumer"
                    },
                    Serializer = new NewtonsoftJsonMessageSerializer() 
                });
}
```
***

## Apache Avro

The <xref:Silverback.Messaging.Serialization.AvroMessageSerializer`1> contained in the [Silverback.Integration.Kafka.SchemaRegistry](https://www.nuget.org/packages/Silverback.Integration.Kafka.SchemaRegistry) package can be used to connect with a schema registry and exchange messages in [Apache Avro](https://avro.apache.org/) format.

# [Fluent](#tab/avro-fluent)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddOutbound<InventoryEvent>(endpoint => endpoint
                    .ProduceTo("inventory-events")
                    .SerializeAsAvro(serializer => serializer
                        .UseType<InventoryEvent>()
                        .Configure(
                            schemaRegistryConfig =>
                            {
                                schemaRegistryConfig.Url = "localhost:8081";
                            },
                            serializerConfig =>
                            {
                                serializerConfig.AutoRegisterSchemas = true;
                            })))
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("order-events")
                    .Configure(config => 
                        {
                            config.GroupId = "my-consumer";
                        })
                    .DeserializeAvro(serializer => serializer
                        .UseType<OrderEvent>()
                        .Configure(
                            schemaRegistryConfig =>
                            {
                                schemaRegistryConfig.Url = "localhost:8081";
                            },
                            serializerConfig =>
                            {
                                serializerConfig.AutoRegisterSchemas = true;
                            }))));
}
```
# [Legacy](#tab/avro-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddOutbound<InventoryEvent>(
                new KafkaProducerEndpoint("inventory-events")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092"
                    },
                    Serializer = new AvroMessageSerializer<InventoryEvent>
                    {
                        SchemaRegistryConfig = new SchemaRegistryConfig
                        {
                            Url = "localhost:8081"
                        },
                        AvroSerializerConfig = new AvroSerializerConfig
                        {
                            AutoRegisterSchemas = true
                        }
                    } 
                })
            .AddInbound(
                new KafkaConsumerEndpoint("order-events")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092",
                        GroupId = "my-consumer"
                    },
                    Serializer = new AvroMessageSerializer<OrderEvent>
                    {
                        SchemaRegistryConfig = new SchemaRegistryConfig
                        {
                            Url = "localhost:8081"
                        },
                        AvroSerializerConfig = new AvroSerializerConfig
                        {
                            AutoRegisterSchemas = true
                        }
                    } 
                });
}
```
***

> [!Note]
> The C# message models can be generated from an Avro schema using [AvroGen](https://www.nuget.org/packages/Confluent.Apache.Avro.AvroGen/).

> [!Note]
> This serializer is built for Kafka but it could work with other brokers, as long as a schema registry is available.

## Custom serializer

In some cases you may want to build your very own custom serializer implementing <xref:Silverback.Messaging.Serialization.IMessageSerializer> directly.

# [Fluent](#tab/custom-fluent)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddOutbound<InventoryEvent>(endpoint => endpoint
                    .ProduceTo("inventory-events")
                    .SerializeUsing(new MyCustomSerializer()))
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("order-events")
                    .Configure(config => 
                        {
                            config.GroupId = "my-consumer";
                        })
                    .DeserializeUsing(new MyCustomSerializer())));
}
```
# [Legacy](#tab/custom-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddOutbound<InventoryEvent>(
                new KafkaProducerEndpoint("inventory-events")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092"
                    },
                    Serializer = new MyCustomSerialzer()
                })
            .AddInbound(
                new KafkaConsumerEndpoint("order-events")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092",
                        GroupId = "my-consumer"
                    },
                    Serializer = new MyCustomSerialzer()
                });
}
```
***


> [!Note]
> You may need to implement `IKafkaMessageSerializer` if you want to have full control over the serialization of the Kafka key as well.

## Binary Files

Please refer to the <xref:binary-files> page if you need to produce or consume raw binary files.
