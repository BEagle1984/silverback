---
uid: endpoint
---

# Endpoint

The endpoint object contains all information that identify the topic/queue that is being connected and all the configurations. The endpoint object is therefore very specific and every broker type will define it's own implementation of `IEndpoint`.

## Kafka

`Silverback.Integration.Kafka` uses 2 different classes to specify inbound and outbound endpoints configuration.

For a more in-depth documentation about the Kafka configuration refer to the [confluent-kafka-dotnet documentation](https://docs.confluent.io/current/clients/confluent-kafka-dotnet/api/Confluent.Kafka.html).

### KafkaProducerEndpoint

Used for outbound endpoints. See <xref:Silverback.Messaging.KafkaProducerEndpoint> for details.

```csharp
new KafkaProducerEndpoint("basket-events")
{
    Configuration = new KafkaProducerConfig
    {
        BootstrapServers = "PLAINTEXT://kafka:9092"
    },
    Chunk = ...
}
```

### KafkaConsumerEndpoint

Used for inbound endpoints. See <xref:Silverback.Messaging.KafkaConsumerEndpoint> for details.

```csharp
new KafkaConsumerEndpoint(
    "order-events", 
    "inventory-events")
{
    Configuration = new KafkaConsumerConfig
    {
        BootstrapServers = "PLAINTEXT://kafka:9092",
        GroupId = "my-consumer",
        AutoOffsetReset = AutoOffsetResetType.Earliest
    }
}
```

> [!Note]
> You can decide whether to use one consumer per topic or subscribe multiple topics with the same consumer (passing multiple topic names in the endpoint constructor, as shown in the example above). There are advantages and disadvantages of both solutions and the best choice really depends on your specific requirements, the amount of messages being produced, etc. Anyway the main difference is that when subscribing multiple topics you will still consume one message after the other but they will simply be interleaved (this may or may not be an issue, it depends) and on the other hand each consumer will use some resources, so creating multiple consumers will result in a bigger overhead.

## RabbitMQ

`Silverback.Integration.RabbitMQ` is a bit more intricated and uses 4 different classes to specify inbound and outbound endpoints configuration.

For a more in-depth documentation about the RabbitMQ configuration refer to the [RabbitMQ tutorials and documentation](https://www.rabbitmq.com/getstarted.html).

### RabbitQueueProducerEndpoint

Used for outbound endpoints that produce directly to a queue. See <xref:Silverback.Messaging.RabbitQueueProducerEndpoint> for details.

```csharp
new RabbitQueueProducerEndpoint("inventory-commands-queue")
{
    Connection = new RabbitConnectionConfig
    {
        HostName = "localhost",
        UserName = "guest",
        Password = "guest"
    },
    Queue = new RabbitQueueConfig
    {
        IsDurable = true,
        IsExclusive = false,
        IsAutoDeleteEnabled = false
    }
}
```

### RabbitExchangeProducerEndpoint

Used for outbound endpoints that produce to an exchange. See <xref:Silverback.Messaging.RabbitExchangeProducerEndpoint> for details.

```csharp
new RabbitExchangeProducerEndpoint("order-events")
{
    Connection = new RabbitConnectionConfig
    {
        HostName = "localhost",
        UserName = "guest",
        Password = "guest"
    },
    Exchange = new RabbitExchangeConfig
    {
        IsDurable = true,
        IsAutoDeleteEnabled = false,
        ExchangeType = ExchangeType.Fanout
    }
}   
```

### RabbitQueueConsumerEndpoint

Used for inbound endpoints that consume directly from a queue. See <xref:Silverback.Messaging.RabbitQueueConsumerEndpoint> for details.

```csharp
new RabbitQueueConsumerEndpoint("inventory-commands-queue")
{
    Connection = new RabbitConnectionConfig
    {
        HostName = "localhost",
        UserName = "guest",
        Password = "guest"
    },
    Queue = new RabbitQueueConfig
    {
        IsDurable = true,
        IsExclusive = false,
        IsAutoDeleteEnabled = false
    }
}
```

### RabbitExchangeConsumerEndpoint

Used for inbound endpoints that consume from an exchange. See <xref:Silverback.Messaging.RabbitExchangeConsumerEndpoint> for details.

```csharp
new RabbitExchangeConsumerEndpoint("order-events")
{
    Connection = new RabbitConnectionConfig
    {
        HostName = "localhost",
        UserName = "guest",
        Password = "guest"
    },
    Exchange = new RabbitExchangeConfig
    {
        IsDurable = true,
        IsAutoDeleteEnabled = false,
        ExchangeType = ExchangeType.Fanout
    },
    QueueName = "my-consumer-group",
    Queue = new RabbitQueueConfig
    {
        IsDurable = true,
        IsExclusive = false,
        IsAutoDeleteEnabled = false
    }
}   
```
